using EmploApiSDK;
using EmploApiSDK.Models;
using EmploFileImport.Mappings;
using EmploApiSDK.Logger;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace EmploFileImport
{
    internal class ImportLogic
    {
        private readonly ILogger _logger;
        private readonly ApiClient _apiClient;
        private readonly ApiConfiguration _apiConfiguration;
        private readonly ApiRequestModelBuilder _apiRequestModelBuilder;

        public ImportLogic(ILogger logger)
        {
            _logger = logger;
            _apiRequestModelBuilder = new ApiRequestModelBuilder(logger);

            _apiConfiguration = new ApiConfiguration()
            {
                EmploUrl = ConfigurationManager.AppSettings["EmploUrl"],
                ApiPath = ConfigurationManager.AppSettings["ApiPath"] ?? "apiv2",
                Login = ConfigurationManager.AppSettings["Login"],
                Password = ConfigurationManager.AppSettings["Password"],
            };

            _apiClient = new ApiClient(_logger, _apiConfiguration);
        }

        public async Task Import(string filePath)
        {
            try
            {
                var importMode = ConfigurationManager.AppSettings["ImportMode"];
                var requireRegistrationForNewEmployees = ConfigurationManager.AppSettings["RequireRegistrationForNewEmployees"];

                ImportUsersRequestModel importUsersRequestModel = BuildRequestModel(importMode, requireRegistrationForNewEmployees, filePath);
                DryRunIfSet(importUsersRequestModel);
                await NormalRunIfSet(importUsersRequestModel);
            }
            catch(Exception exception)
            {
                _logger.WriteLine(exception.Message);
            }
        }

        private void DryRunIfSet(ImportUsersRequestModel importUsersRequestModel)
        {
            bool dryRun;
            if(bool.TryParse(ConfigurationManager.AppSettings["DryRun"], out dryRun) && dryRun)
            {
                _logger.WriteLine("Importer is in DryRun mode, data retrieved from file will be printed to log, but it won't be send to emplo.");
                var serializedData = JsonConvert.SerializeObject(importUsersRequestModel.Rows);
                _logger.WriteLine(serializedData);

                Environment.Exit(0);
            }
        }

        private async Task NormalRunIfSet(ImportUsersRequestModel importUsersRequestModel)
        {
            int chunkSize = GetChunkSize();
            _logger.WriteLine(String.Format("Sending employee data to emplo (in chunks in size of {0})", chunkSize));

            // first, send data without superiors
            foreach(var chunk in Chunk(importUsersRequestModel.Rows, chunkSize))
            {
                var importUsersRequestModelChunk = new ImportUsersRequestModel(importUsersRequestModel.Mode, importUsersRequestModel.RequireRegistrationForNewEmployees)
                {
                    ImportId = importUsersRequestModel.ImportId,
                    Rows = chunk.ToList()
                };
                var serializedData = JsonConvert.SerializeObject(importUsersRequestModelChunk);
                var importValidationSummary = await _apiClient.SendPostAsync<ImportUsersResponseModel>(serializedData, _apiConfiguration.ImportUsersUrl);
                if(importValidationSummary.ImportStatusCode != ImportStatusCode.Ok)
                {
                    _logger.WriteLine("Import action returned error status: " + importValidationSummary.ImportStatusCode);
                    Environment.Exit(-1);
                }
                importUsersRequestModel.ImportId = importValidationSummary.ImportId;
                SaveImportValidationSummaryLog(importValidationSummary);
            }

            if(importUsersRequestModel.Rows.Any())
            {
                _logger.WriteLine("Finishing import...");
                FinishImportRequestModel requestModel = new FinishImportRequestModel(ConfigurationManager.AppSettings["BlockSkippedUsers"]);
                requestModel.ImportId = importUsersRequestModel.ImportId;
                var serializedData = JsonConvert.SerializeObject(requestModel);
                var finishImportResponse = await _apiClient.SendPostAsync<FinishImportResponseModel>(serializedData, _apiConfiguration.FinishImportUrl);
                if(finishImportResponse.ImportStatusCode != ImportStatusCode.Ok)
                {
                    _logger.WriteLine("FinishImport action returned error status: " + finishImportResponse.ImportStatusCode);
                    Environment.Exit(-1);
                }
                else
                {
                    if(finishImportResponse.BlockedUserIds != null && finishImportResponse.BlockedUserIds.Any())
                    {
                        _logger.WriteLine("Blocked user id's: " + String.Join(", ", finishImportResponse.BlockedUserIds));
                    }

                    if(finishImportResponse.UpdateUnitResults != null && finishImportResponse.UpdateUnitResults.Any())
                    {
                        _logger.WriteLine("Units tree was updated:");
                        foreach(var message in finishImportResponse.UpdateUnitResults)
                        {
                            if(message.IsError)
                            {
                                _logger.WriteLine(String.Format("Unit update error: {0}", message.Message));
                            }
                            else
                            {
                                _logger.WriteLine(String.Format("Unit updated: unit {0} was updated, old parent={1}, new parent={2}, message: {3}",
                                    message.UpdatedUnitId, message.OldParentId, message.NewParentId, message.Message));
                            }
                        }
                    }
                }
            }

            _logger.WriteLine("Import has finished successfully");
        }

        private ImportUsersRequestModel BuildRequestModel(string importMode, string requireRegistrationForNewEmployees, string filePath)
        {
            ImportUsersRequestModel importUsersRequestModel = new ImportUsersRequestModel(importMode, requireRegistrationForNewEmployees)
            {
                Rows = new List<UserDataRow>()
            };

            var separator = ConfigurationManager.AppSettings["Separator"].FirstOrDefault();
            var headerLineNumber = Int32.Parse(ConfigurationManager.AppSettings["HeaderLineNumber"]);
            var fileParser = new FileParser(filePath, separator, headerLineNumber);

            var rowsValues = fileParser.ReadValues();
            foreach(var rowValues in rowsValues)
            {
                var newUserDatarow = _apiRequestModelBuilder.BuildUserDataRow(rowValues);
                importUsersRequestModel.Rows.Add(newUserDatarow);
            }

            return importUsersRequestModel;
        }

        private IEnumerable<IEnumerable<T>> Chunk<T>(IEnumerable<T> source, int chunksize)
        {
            while(source.Any())
            {
                yield return source.Take(chunksize);
                source = source.Skip(chunksize);
            }
        }

        private void SaveImportValidationSummaryLog(ImportUsersResponseModel importValidationSummary)
        {
            if(!importValidationSummary.OperationResults.Any())
            {
                _logger.WriteLine("Result is empty.");
            }

            foreach(var result in importValidationSummary.OperationResults)
            {
                string employeeHeader = String.Format("{0} (id={1})", result.Employee, result.EmployeeId);

                if(result.StatusCode == ImportStatuses.Skipped)
                {
                    _logger.WriteLine(String.Format("{0} SKIPPED - {1}", employeeHeader, result.Message));
                    continue;
                }

                if(result.StatusCode != ImportStatuses.Ok)
                {
                    string error = String.Format("Status: {0}, ErrorColumns: {1}, Message: {2}", result.StatusCode, String.Join(",", result.ErrorColumns), result.Message);
                    _logger.WriteLine(String.Format("{0} ERROR - {1}", employeeHeader, error));
                    continue;
                }

                string changedColumns = String.Join(", ", result.ChangedColumns);
                if(result.Created)
                {
                    _logger.WriteLine(String.Format("{0} CREATED - Changed columns: {1}", employeeHeader, changedColumns));
                }
                else
                {
                    if(changedColumns.Any())
                    {
                        _logger.WriteLine(String.Format("{0} UPDATED - Updated columns: {1}", employeeHeader, changedColumns));
                    }
                    else
                    {
                        _logger.WriteLine(String.Format("{0} NO CHANGES", employeeHeader));
                    }
                }
            }
        }

        private int GetChunkSize()
        {
            string sizeString = ConfigurationManager.AppSettings["ChunkSize"] ?? "";
            int size;
            if(Int32.TryParse(sizeString, out size))
                return size;
            return 5;
        }
    }
}
