using EmploFileImport.Mappings;
using EmploApiSDK.Logger;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using EmploApiSDK.ApiModels.Employees;
using EmploApiSDK.Logic.EmployeeImport;
using Newtonsoft.Json;

namespace EmploFileImport
{
    internal class EmployeeFileImportLogic
    {
        private readonly ILogger _logger;
        private readonly ApiRequestModelBuilder _apiRequestModelBuilder;
        private readonly ImportLogic _importLogic;

        public EmployeeFileImportLogic(ILogger logger)
        {
            _logger = logger;
            _apiRequestModelBuilder = new ApiRequestModelBuilder(logger);

            _importLogic = new ImportLogic(_logger);
        }

        public async Task Import(string filePath)
        {
            try
            {
                var importMode = ConfigurationManager.AppSettings["ImportMode"];
                var requireRegistrationForNewEmployees =
                    ConfigurationManager.AppSettings["RequireRegistrationForNewEmployees"];

                ImportUsersRequestModel importUsersRequestModel =
                    BuildRequestModel(importMode, requireRegistrationForNewEmployees, filePath);
                DryRunIfSet(importUsersRequestModel);
                var result = await _importLogic.ImportEmployees(importUsersRequestModel);
                if (result == -1)
                {
                    Environment.Exit(-1);
                }
            }
            catch (EmploApiClientFatalException)
            {
                Environment.Exit(-1);
            }
            catch(Exception exception)
            {
                _logger.WriteLine(exception.Message, LogLevelEnum.Error);
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
    }
}
