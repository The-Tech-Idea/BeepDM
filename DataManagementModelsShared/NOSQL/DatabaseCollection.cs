using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.DataManagment_Engine.NOSQL
{
    public class DatabaseCollection
    {
        public DatabaseCollection()
        { }
        public int CountOfDocuments { get; set; }
        public string DatabasName { get; set; }
        public List<string> Collections { get; set; } = new List<string>();
    }
}
