
using System;
using System.Collections.Generic;
using System.Text;


namespace TheTechIdea.Beep.Environments
{
    public interface IBeepEnvironment
    {
        string GuidID { get; set; }
        string EnvironmentName { get; set; }
        string EnvironmentDescription { get; set; }
        EnvironmentType EnvironmentType { get; set; }
        string EnvironmentVersion { get; set; }
        string EnvironmentPath { get; set; }
        string EnvironmentIcon { get; set; }
        string EnvironmentIconPath { get; set; }
        Dictionary<string, string> Datasources { get; set; } // Key,Value
        Dictionary<string, string> Services { get; set; }// Key,Value
        Dictionary<string, string> DataModels { get; set; }// Key,Value

    }

}
