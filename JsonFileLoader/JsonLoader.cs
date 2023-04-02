using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
            var settings = new JsonSerializerSettings
            {
         //       TypeNameHandling = TypeNameHandling.All,
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
           //     CheckAdditionalContent = true,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }

            };

            if (File.Exists(filename))
            {
                String JSONtxt = File.ReadAllText(filename);

                return  JsonConvert.DeserializeObject<List<T>>(JSONtxt,settings);
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
            //    TypeNameHandling = TypeNameHandling.All,
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
            //    CheckAdditionalContent = true,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }

            };

            return JsonConvert.DeserializeObject<List<T>>(jsonString, settings);
           

        }
        public T DeserializeSingleObjectFromjsonString<T>(string jsonString)
        {
            var settings = new JsonSerializerSettings
            {
              //  TypeNameHandling = TypeNameHandling.All,
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
              //  CheckAdditionalContent = true,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }

            };

            return JsonConvert.DeserializeObject<T>(jsonString, settings);


        }
        public T DeserializeSingleObject<T>(string filename)
        {
            var settings = new JsonSerializerSettings
            {
               // TypeNameHandling = TypeNameHandling.All,
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
             //   CheckAdditionalContent = true,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }

            };
            try
            {
                if (File.Exists(filename))
                {
                    String JSONtxt = File.ReadAllText(filename);

                    return JsonConvert.DeserializeObject<T>(JSONtxt,settings);
                }
                else
                    return default(T);

            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }

            return default(T);
        }
        public void Serialize(string filename, object t)
        {

            using (StreamWriter file = File.CreateText(filename))
            {
                try
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.NullValueHandling = NullValueHandling.Ignore;
                    serializer.MissingMemberHandling = MissingMemberHandling.Ignore;
                    serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    serializer.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                    serializer.Serialize(file, t);
                }
                catch (Exception ex)
                {

                   Console.WriteLine(ex.ToString());

                }
               
            }
        }
        public object DeserializeObjectString<T>(string stringobject)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                //CheckAdditionalContent=true,
                //TypeNameHandling = TypeNameHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter()}
                

            };
            return JsonConvert.DeserializeObject<object>(stringobject, settings);
        }
        public  DataTable JsonToDataTable(string jsonString)
        {
            DataTable dt = null;
            DataSet ds= ConverttoDataset(jsonString);
            if(ds!=null)
            {
                if(ds.Tables.Count>0)
                {
                    dt = ds.Tables[0];

                }
            }
          
           
            return dt;
        }
        public DataSet ConverttoDataset(string jsonString)
        {
          

            // Deserialize the JSON string to a JArray
            JArray jsonArray = JsonConvert.DeserializeObject<JArray>(jsonString);
            DataTable dataTable = new DataTable();
            // Create a new DataSet
            DataSet dataSet = new DataSet();
            foreach (JToken token in jsonArray)
            {
                if (token.Type == JTokenType.Object)
                {
                    JObject jsonObject = token as JObject;
                    if (jsonObject != null)
                    {
                        if (dataTable.Columns.Count == 0)
                        {
                            foreach (JProperty property in jsonObject.Properties())
                            {
                                dataTable.Columns.Add(property.Name, typeof(string));
                            }
                        }
                        DataRow dataRow = dataTable.NewRow();
                        foreach (JProperty property in jsonObject.Properties())
                        {
                            try
                            {
                                dataRow[property.Name] = property.Value.ToString();
                            }
                            catch (Exception ex1)
                            {
                                dataTable.Columns.Add(property.Name, typeof(string));
                                dataRow[property.Name] = property.Value.ToString();


                            }
                          
                        }
                        dataTable.Rows.Add(dataRow);
                    }
                }
            }
            dataSet.Tables.Add(dataTable);
            return dataSet;
        }
        public string CheckJsonType(JToken obj)
        {
          
                return obj.Type.ToString();
           
        }
        
    }
}
