using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Editor
{
   
    public class BeepEventDataArgs : EventArgs
    {
        public string EventName { get; set; }
        public object EventData { get; set; }
        public bool Cancel { get; set; } = false;
        public IEntityStructure EntityStructure { get; set; }
        public string EntityName { get; set; }
        public string DataSourceName { get; set; }
        public string PassedValueString { get; set; }
        public object PassedValueObject { get; set; }
        public string PassedValueString2 { get; set; }
        public object PassedValueObject2 { get; set; }
        public string PassedValueString3 { get; set; }
        public object PassedValueObject3 { get; set; }
        public string PassedValueString4 { get; set; }
        public object PassedValueObject4 { get; set; }
        public int PassedValueInt { get; set; }
        public int PassedValueInt2 { get; set; }
        public int PassedValueInt3 { get; set; }
        public int PassedValueInt4 { get; set; }
        public double PassedValueDouble { get; set; }
        public double PassedValueDouble2 { get; set; }
        public double PassedValueDouble3 { get; set; }
        public double PassedValueDouble4 { get; set; }
        public bool PassedValueBool { get; set; }
        public bool PassedValueBool2 { get; set; }
        public bool PassedValueBool3 { get; set; }
        public bool PassedValueBool4 { get; set; }
        public DateTime PassedValueDateTime { get; set; }
        public DateTime PassedValueDateTime2 { get; set; }
        public DateTime PassedValueDateTime3 { get; set; }
        public DateTime PassedValueDateTime4 { get; set; }
        public List<string> PassedValueListString { get; set; }
        public List<object> PassedValueListObject { get; set; }
        public List<int> PassedValueListInt { get; set; }
        public List<double> PassedValueListDouble { get; set; }
        public List<bool> PassedValueListBool { get; set; }
        public List<DateTime> PassedValueListDateTime { get; set; }
        public List<IEntityStructure> PassedValueListEntityStructure { get; set; }



        public IPassedArgs Args { get; set; }
        public BeepEventDataArgs(string eventName, object eventData)
        {
            EventName = eventName;
            EventData = eventData;
        }

    }
}
