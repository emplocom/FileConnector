using EmploApiSDK.Logger;
using System;
using System.Configuration;

namespace EmploFileImport.Validators
{
    class SettingsValidator
    {
        public static void ValidateSettings()
        {
            var isValid = true;
            var baseUrl = ConfigurationManager.AppSettings["EmploUrl"];
            if (String.IsNullOrEmpty(baseUrl))
            {
                LoggerFactory.Instance.WriteLine("EmploUrl is empty. Example url : https://example.emplo.com");
                isValid = false;
            }

            var emploLogin = ConfigurationManager.AppSettings["Login"];
            if (String.IsNullOrEmpty(emploLogin))
            {
                LoggerFactory.Instance.WriteLine("Login to emplo is empty");
                isValid = false;
            }

            var emploPassword = ConfigurationManager.AppSettings["Password"];
            if (String.IsNullOrEmpty(emploPassword))
            {
                LoggerFactory.Instance.WriteLine("Password to emplo is empty");
                isValid = false;
            }

            var separator = ConfigurationManager.AppSettings["Separator"];
            if(String.IsNullOrEmpty(separator))
            {
                LoggerFactory.Instance.WriteLine("Separator is empty");
                isValid = false;
            }

            var headerLineNumber = ConfigurationManager.AppSettings["HeaderLineNumber"];
            if(String.IsNullOrEmpty(headerLineNumber) && Int32.TryParse(headerLineNumber, out int number))
            {
                LoggerFactory.Instance.WriteLine("HeaderLineNumber is empty or not number");
                isValid = false;
            }

            if (!isValid)
            {
                LoggerFactory.Instance.WriteLine("Update config file and try again.");
                LoggerFactory.Instance.WriteLine("Emplo import stopped because of configuration errors");
                Environment.Exit(-1);
            }
        }
    }
}
