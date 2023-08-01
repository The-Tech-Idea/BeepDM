using DataManagementModels.DriversConfigurations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Connections
{
    public static class UtilConnections
    {
      
        public static string ReplaceValueFromConnectionString(ConnectionDriversConfig DataSourceDriver , IConnectionProperties ConnectionProp,IDMEEditor DMEEditor)
        {
            bool IsConnectionString = false;
            bool IsUrl = false;
            bool IsFile = false;
            string rep = "";
            string input = ""; 
            string replacement;
            string pattern;

            if ( string.IsNullOrWhiteSpace(ConnectionProp.ConnectionString))
            {
                if (!string.IsNullOrEmpty(DataSourceDriver.ConnectionString))
                {
                    IsConnectionString = true;
                    ConnectionProp.ConnectionString = DataSourceDriver.ConnectionString;

                }
            }
            else
            {
                IsConnectionString = true;
            }
            
            if (!string.IsNullOrWhiteSpace(ConnectionProp.Url))
            {
                IsUrl = true;

            }
            if (!string.IsNullOrWhiteSpace(ConnectionProp.FilePath) || !string.IsNullOrWhiteSpace(ConnectionProp.FileName))
            {
               
                IsFile = true;
            }
            if (IsConnectionString)
            {
              
                input = ConnectionProp.ConnectionString;
            }
            
            if (IsUrl)
            {
                input = ConnectionProp.Url;
                 pattern = "{Url}";
                 replacement = ConnectionProp.Url ?? string.Empty; ;
                input = Regex.Replace(input, pattern, replacement, RegexOptions.IgnoreCase);
               
            }
            if(IsFile)
            {
                if (ConnectionProp.FilePath.StartsWith(".") || ConnectionProp.FilePath.Equals("/") || ConnectionProp.FilePath.Equals("\\"))
                {
                    ConnectionProp.FilePath = ConnectionProp.FilePath.Replace(".", DMEEditor.ConfigEditor.ExePath);
                }
               // input= Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName);
            }
           

          

             pattern = "{Host}";
             replacement = ConnectionProp.Host ?? string.Empty;
            input = Regex.Replace(input, pattern, replacement, RegexOptions.IgnoreCase);

            pattern = "{UserID}";
            replacement = ConnectionProp.UserID ?? string.Empty;
            input = Regex.Replace(input, pattern, replacement, RegexOptions.IgnoreCase);

            pattern = "{Password}";
            replacement = ConnectionProp.Password ?? string.Empty;
            input = Regex.Replace(input, pattern, replacement, RegexOptions.IgnoreCase);

            pattern = "{DataBase}";
            replacement = ConnectionProp.Database ?? string.Empty;
            input = Regex.Replace(input, pattern, replacement, RegexOptions.IgnoreCase);

            pattern = "{Port}";
            replacement = ConnectionProp.Port.ToString() ?? string.Empty;
            input = Regex.Replace(input, pattern, replacement, RegexOptions.IgnoreCase);






            if (IsFile)
            {
                if (!string.IsNullOrWhiteSpace(ConnectionProp.ConnectionString))
                {

                    pattern = "{File}";
                    replacement = Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName) ?? string.Empty;
                    input = Regex.Replace(input, pattern, replacement, RegexOptions.IgnoreCase);
                }
                else
                {
                    input = Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName);
                }
            }


            rep = input;
            return rep;
        }
    }
}
