using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.DataBase
{
    public class ChildRelation : IChildRelation
    {
        public ChildRelation()
        {

        }
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string child_table { get ; set ; }
        public string child_column { get ; set ; }
        public string parent_table { get ; set ; }
        public string parent_column { get ; set ; }
        public string Constraint_Name { get ; set ; }
        public string RalationName { get ; set ; }
    }
}
