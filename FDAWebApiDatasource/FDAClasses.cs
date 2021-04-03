using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.DataManagment_Engine.WebAPI.FDAWebApi
{
    

        public class Rootobject
        {
            public Meta meta { get; set; }
            public Result[] results { get; set; }
        }

        public class Meta
        {
            public string disclaimer { get; set; }
            public string terms { get; set; }
            public string license { get; set; }
            public string last_updated { get; set; }
            public Results results { get; set; }
        }

        public class Results
        {
            public int skip { get; set; }
            public int limit { get; set; }
            public int total { get; set; }
        }

        public class Result
        {
            public string report_number { get; set; }
            public string[] outcomes { get; set; }
            public string date_created { get; set; }
            public string[] reactions { get; set; }
            public string date_started { get; set; }
            public Consumer consumer { get; set; }
            public Product[] products { get; set; }
        }

        public class Consumer
        {
            public string age { get; set; }
            public string age_unit { get; set; }
            public string gender { get; set; }
        }

        public class Product
        {
            public string role { get; set; }
            public string name_brand { get; set; }
            public string industry_code { get; set; }
            public string industry_name { get; set; }
        }

   
}
