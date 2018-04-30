using EmploApiSDK.Logger;
using System;
using System.Collections.Generic;
using System.Configuration;
using EmploApiSDK.Logic.EmployeeImport;

namespace EmploFileImport.Mappings
{
    internal class FileImportMappingConfiguration : BaseImportConfiguration
    {
        public FileImportMappingConfiguration(ILogger logger) : base(logger)
        {
        }
    }
}
