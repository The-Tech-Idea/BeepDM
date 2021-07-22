using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Util
{
    public class JsonLoader : IJsonLoader
    {
        public JsonFieldsCollector fieldsCollector { get; set; }
        public JsonLoader()
        {

        }
        public List<T> DeserializeObject<T>(string filename)
        {
           
            if (File.Exists(filename))
            {
                String JSONtxt = File.ReadAllText(filename);

                return  JsonConvert.DeserializeObject<List<T>>(JSONtxt);
            }else
            {

                List<T> lists = new List<T>();
                return lists;
            }

        }
        public List<T> DeserializeObjectFromjsonString<T>(string jsonString)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                CheckAdditionalContent = true

            };

            return JsonConvert.DeserializeObject<List<T>>(jsonString, settings);
           

        }
        public T DeserializeSingleObject<T>(string filename)
        {

            if (File.Exists(filename))
            {
                String JSONtxt = File.ReadAllText(filename);

                return JsonConvert.DeserializeObject<T>(JSONtxt);
            }
            else
                return default(T);
            
        }
        public void Serialize(string filename, object t)
        {
          
            using (StreamWriter file = File.CreateText(filename))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, t);
            }
        }
        public object DeserializeObjectString<T>(string stringobject)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                 CheckAdditionalContent=true

            };
            return JsonConvert.DeserializeObject<object>(stringobject, settings);
        }
        public  DataTable JsonToDataTable(string jsonString)
        {
            //var jsonLinq = JObject.Parse(jsonString);

            //// Find the first array using Linq  
            //var srcArray = jsonLinq.Descendants().Where(d => d is JArray).First();
            //var trgArray = new JArray();
            //foreach (JObject row in srcArray.Children<JObject>())
            //{
            //    var cleanRow = new JObject();
            //    foreach (JProperty column in row.Properties())
            //    {
            //        // Only include JValue types  
            //        if (column.Value is JValue)
            //        {
            //            cleanRow.Add(column.Name, column.Value);
            //        }
            //    }
            //    trgArray.Add(cleanRow);
            //}
            DataTable dt;
            try
            {
                dt= JsonConvert.DeserializeObject<DataTable>(jsonString);
            }
            catch (Exception ex)
            {
                try
                {
                    dt = ConverttoDataset(jsonString).Tables[0];
                }
                catch (Exception ex1)
                {

                    dt = null;
                }
              
            }
            return dt;
        }
        public DataSet ConverttoDataset(string jsonString)
        {
            return JsonConvert.DeserializeObject<DataSet>(jsonString);
        }
        public string CheckJsonType(JToken obj)
        {
          
                return obj.Type.ToString();
           
        }
        
    }
}
