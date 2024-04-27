using DataManagementModels.DriversConfigurations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Helpers
{
    public static class FileHelper
    {
        public static string GetFileExtensions(IDMEEditor DMEEditor)
        {
            List<ConnectionDriversConfig> clss = DMEEditor.ConfigEditor.DataDriversClasses.Where(p => p.extensionstoHandle != null).ToList();
            string retval = null;
            if (clss != null)
            {
                IEnumerable<string> extensionslist = clss.Select(p => p.extensionstoHandle);
                string extstring = string.Join(",", extensionslist);
                List<string> exts = extstring.Split(',').Distinct().ToList();

                foreach (string item in exts)
                {
                    retval += item + " files(*." + item + ")|*." + item + "|";


                }

            }

            retval += "All files(*.*)|*.*";
            return retval;
        }
        public static List<ConnectionDriversConfig> GetFileDataSources(IDMEEditor dMEEditor)
        {
            List<ConnectionDriversConfig> clss = dMEEditor.ConfigEditor.DataDriversClasses.Where(p => p.extensionstoHandle != null).ToList();
            return clss;
        }
        public static ConnectionProperties FileExists(IDMEEditor DMEEditor, string fileandpath)
        {
            ConnectionProperties cn=null;
            try
            {
                string extens = GetFileExtensions(DMEEditor);
                string filename=Path.GetFileName(fileandpath);
                string filepath = Path.GetDirectoryName(fileandpath);
                 cn= DMEEditor.ConfigEditor.DataConnections.Where(p=>p.Category== DatasourceCategory.FILE && p.FileName == filename && p.FilePath == filepath).FirstOrDefault();
              
                   
              
              
            }
            catch (Exception ex)
            {
                string mes = ex.Message;
                DMEEditor.AddLogMessage(ex.Message, "Could not Load Files ", DateTime.Now, -1, mes, Errors.Failed);
                 cn=null;
            };
            return cn;
        }
        public static List<ConnectionProperties> LoadFiles(IDMEEditor DMEEditor, List<string> filenames)
        {

            List<ConnectionProperties> retval = new List<ConnectionProperties>();
            try
            {
                retval = DMEEditor.Utilfunction.LoadFiles(filenames.ToArray());
            }
            catch (Exception ex)
            {
                string mes = ex.Message;
                DMEEditor.AddLogMessage(ex.Message, "Could not Load Files ", DateTime.Now, -1, mes, Errors.Failed);
                return null;
            };
            return retval;
        }
        public static ConnectionDriversConfig ExtensionExists(IDMEEditor DMEEditor, string ext)
        {
            List<ConnectionDriversConfig> clss = DMEEditor.ConfigEditor.DataDriversClasses.Where(p => p.extensionstoHandle != null).ToList();
            if (clss != null)
            {
                IEnumerable<string> extensionslist = clss.Select(p => p.extensionstoHandle);
                string extstring = string.Join(",", extensionslist);
                List<string> exts = extstring.Split(',').Distinct().ToList();
                foreach (ConnectionDriversConfig cls in clss)
                {
                    if (exts.Contains(ext))
                    {
                        return cls;
                    }
                }
               
            }
            return null;
        }



    }
}
