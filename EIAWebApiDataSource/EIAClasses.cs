using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.DataManagment_Engine.WebAPI.EIAWebApi
{

    public class Rootobject
    {
        public Request request { get; set; }
        public Category category { get; set; }
    }

    public class Request
    {
        public int category_id { get; set; }
        public string command { get; set; }
    }

    public class Category
    {
        public string category_id { get; set; }
        public string parent_category_id { get; set; }
        public string name { get; set; }
        public string notes { get; set; }
        public Childcategory[] childcategories { get; set; }
        public Childsery[] childseries { get; set; }
    }

    public class Childcategory
    {
        public int category_id { get; set; }
        public string name { get; set; }
    }

    public class Childsery
    {
        public string series_id { get; set; }
        public string name { get; set; }
        public string f { get; set; }
        public string units { get; set; }
        public string updated { get; set; }
    }

}
