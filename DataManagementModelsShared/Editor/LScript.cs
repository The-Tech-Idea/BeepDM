﻿using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Editor
{
    public class LScriptHeader
    {
        public LScriptHeader()
        {
            id = Guid.NewGuid().ToString();
        }
        public List<LScript> Scripts { get; set; } = new List<LScript>();
        public string workflow { get; set; }
        public string scriptSource { get; set; }
        public string id { get; set; }
        

    }
    public class LScript : ILScript
    {
        public LScript()
        {
            id = Guid.NewGuid().ToString();
        }
        public string id { get; set; }
        public string ddl { get ; set ; }
        public string entityname { get ; set ; }
        public string destinationdatasourcename { get ; set ; }
        public string sourcedatasourcename { get; set; }
        public string errormessage { get ; set ; }
        public bool Active { get; set; } = false;
        public IErrorsInfo errorsInfo { get; set; }
        public DDLScriptType scriptType { get; set; }
      //  public List<LScriptTracker> trackers { get; set; } = new List<LScriptTracker>();
         
    }
    public class LScriptTrackHeader
    {
        public LScriptTrackHeader()
        {

        }  
        public string parentscriptHeaderid { get; set; }
        public DateTime rundate { get; set; }
        public List<LScriptTracker> trackingscript { get; set; } = new List<LScriptTracker>();
    }
    public class LScriptTracker
    {
      
        public string parentscriptid { get; set; }
        public string currentrecorddatasourcename { get; set; }
        public int currenrecordindex { get; set; }
        public string currenrecordentity { get; set; }
        public string errormessage { get; set; }
        public IErrorsInfo errorsInfo { get; set; }
        public DDLScriptType scriptType { get; set; }


    }
}