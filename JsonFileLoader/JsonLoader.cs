using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.ConfigUtil;

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

    }
}
