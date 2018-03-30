using EmploApiSDK.Models;
using EmploFileImport.Models;
using EmploApiSDK.Logger;

namespace EmploFileImport.Mappings
{
    internal class ApiRequestModelBuilder
    {
        private readonly FileImportMappingConfiguration _fileImportMappingConfiguration;

        public ApiRequestModelBuilder(ILogger logger)
        {
            _fileImportMappingConfiguration = new FileImportMappingConfiguration(logger);
        }

        public UserDataRow BuildUserDataRow(ValuesRow rowValues)
        {
            var importedEmployeeRow = new UserDataRow();
            var rowValuesItems = rowValues.Get();
            foreach(ValueHeaderName item in rowValuesItems)
            {
                foreach(var mapping in _fileImportMappingConfiguration.PropertyMappings)
                {
                    if(NormalizeString(mapping.FileHeaderName) == NormalizeString(item.HeaderName))
                    {
                        importedEmployeeRow.Add(mapping.EmploPropertyName, NormalizeString(item.Value));
                    }
                }
            }
            return importedEmployeeRow;
        }

        private string NormalizeString(string @string)
        {
            return @string.Trim('\t', ' ');
        }
    }
}
