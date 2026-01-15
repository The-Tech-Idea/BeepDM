using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.Environments.Data
{
    public class BeepEnvironment : IBeepEnvironment
    {
        public BeepEnvironment()
        {
            GuidID= Guid.NewGuid().ToString();
        }
        public BeepEnvironment(string envname, string envdesc, EnvironmentType envtype, string envversion)
        {
            GuidID = Guid.NewGuid().ToString();
        }
        public BeepEnvironment(string envname, string envdesc, EnvironmentType envtype, string envversion, string envpath, string envicon, string enviconpath)
        {
            GuidID = Guid.NewGuid().ToString();
            EnvironmentName = envname;
            EnvironmentDescription = envdesc;
            EnvironmentType = envtype;
            EnvironmentVersion = envversion;
            EnvironmentPath = envpath;
            EnvironmentIcon = envicon;
            EnvironmentIconPath = enviconpath;
        }
        public BeepEnvironment(string envname, string envdesc, EnvironmentType envtype, string envversion, string envpath, string envicon, string enviconpath, Dictionary<string, string> datasources, Dictionary<string, string> services, Dictionary<string, string> datamodels)
        {
            GuidID = Guid.NewGuid().ToString();
            EnvironmentName = envname;
            EnvironmentDescription = envdesc;
            EnvironmentType = envtype;
            EnvironmentVersion = envversion;
            EnvironmentPath = envpath;
            EnvironmentIcon = envicon;
            EnvironmentIconPath = enviconpath;
            Datasources = datasources;
            Services = services;
            DataModels = datamodels;
        }
        public BeepEnvironment(string envname, string envdesc, EnvironmentType envtype, string envversion, string envpath, string envicon, string enviconpath, Dictionary<string, string> datasources, Dictionary<string, string> services, Dictionary<string, string> datamodels, string guid)
        {
            GuidID = guid;
            EnvironmentName = envname;
            EnvironmentDescription = envdesc;
            EnvironmentType = envtype;
            EnvironmentVersion = envversion;
            EnvironmentPath = envpath;
            EnvironmentIcon = envicon;
            EnvironmentIconPath = enviconpath;
            Datasources = datasources;
            Services = services;
            DataModels = datamodels;
        }
        public string GuidID { get; set; }

        public string EnvironmentName { get ; set ; }
        public string EnvironmentDescription { get ; set ; }
        public EnvironmentType EnvironmentType { get ; set ; }
        public string EnvironmentVersion { get ; set ; }
        public string EnvironmentPath { get ; set ; }
        public string EnvironmentIcon { get ; set ; }
        public string EnvironmentIconPath { get ; set ; }
        public Dictionary<string, string> Datasources { get ; set ; }
        public Dictionary<string, string> Services { get ; set ; }
        public Dictionary<string, string> DataModels { get ; set ; }
    }
}