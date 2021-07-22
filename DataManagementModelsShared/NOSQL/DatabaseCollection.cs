using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.NOSQL
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
