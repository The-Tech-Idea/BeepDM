using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace TheTechIdea.DataManagment_Engine.AI
{
    public interface IAAPP
    {
        string AppName { get; set; }
        string AppID { get; }
        string DataSourceName { get; set; }
        List<string> Tables { get; set; }
        List<DataTable> TestData { get; set; }
        IDMEEditor DMEEditor { get; set; }



    }
    public interface IAAPPTestData
    {
        string name { get; set; }
        string DataSourceName { get; set; }
        string EntityName { get; set; }
        List<string> Inputvectors { get; set; }

        List<string> Outputvectors { get; set; }
    }
    public sealed class MLMethod : Attribute
    {
        public string Caption { get; set; }
        public bool Hidden { get; set; } = false;
        public bool Click { get; set; } = false;
        public bool DoubleClick { get; set; } = false;
    }
    public sealed class MLPredict : Attribute
    {
        public string Caption { get; set; }
        public bool Hidden { get; set; } = false;
        public bool Click { get; set; } = false;
        public bool DoubleClick { get; set; } = false;
    }
    public sealed class MLLoadModule : Attribute
    {
        public string Caption { get; set; }
        public bool Hidden { get; set; } = false;
        public bool Click { get; set; } = false;
        public bool DoubleClick { get; set; } = false;
    }
    public sealed class MLEval : Attribute
    {
        public string Caption { get; set; }
        public bool Hidden { get; set; } = false;
        public bool Click { get; set; } = false;
        public bool DoubleClick { get; set; } = false;
    }
    public sealed class MLLoadData : Attribute
    {
        public string Caption { get; set; }
        public bool Hidden { get; set; } = false;
        public bool Click { get; set; } = false;
        public bool DoubleClick { get; set; } = false;
    }
    public sealed class MLSplitData : Attribute
    {
        public string Caption { get; set; }
        public bool Hidden { get; set; } = false;
        public bool Click { get; set; } = false;
        public bool DoubleClick { get; set; } = false;
    }
    public class PredictionBase
    {
      


    }
}
