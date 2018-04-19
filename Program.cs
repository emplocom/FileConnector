using EmploFileImport.Validators;
using EmploApiSDK.Logger;
using System;
using System.IO;
using System.Linq;

namespace EmploFileImport
{
    class Program
    {
        // Niestety async Main jest dopierwo w C# 7.0
        static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        private static async System.Threading.Tasks.Task MainAsync(string[] args)
        {
            ILogger logger = LoggerFactory.CreateLogger(args);
            logger.WriteLine("Import to emplo started");

            SettingsValidator.ValidateSettings();

            try
            {
                string filePath = null;
                bool importFileExists = false;
#if DEBUG
                importFileExists = true;
                filePath = "import.txt";
#endif
#if RELEASE
                importFileExists = args.Any() && (filePath = TryGettingFilePath(args[0])) != string.Empty;
#endif

                if(importFileExists)
                {
                    EmployeeFileImportLogic employeeFileImportLogic = new EmployeeFileImportLogic(logger);
                    await employeeFileImportLogic.Import(filePath);
                    Console.WriteLine("Job's done. Press any key");
                }
                else
                {
                    Console.WriteLine("No import filePath as argument");
                    logger.WriteLine("No filePath as argument");
                }
            }
            catch(Exception ex)
            {
                logger.WriteLine(ex.Message);
            }
            Console.ReadKey();
        }

        private static string TryGettingFilePath(string path)
        {
            bool exists = File.Exists(path);
            if(!exists)
            {
                path = Environment.CurrentDirectory + "\\" + path;
                exists = File.Exists(path);
            }

            return exists ? path : string.Empty;
        }
    }
}
