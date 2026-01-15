
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using TheTechIdea.Beep.ConfigUtil;
 
namespace TheTechIdea.Beep.JsonLoaderService
{/// <summary>
/// Provides methods for serializing objects to JSON and deserializing JSON to objects.
/// </summary>
    public class JsonLoader : IJsonLoader,IDisposable
    {
       
        private bool disposedValue;

        /// <summary>
        /// Initializes a new instance of the JsonLoader class.
        /// </summary>
        public JsonLoader()
        {

        }
        /// <summary>
        /// Serializes the specified object to a JSON string.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>A JSON string representation of the object.</returns>
        public string SerializeObject(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
        /// <summary>
        /// Deserializes the JSON string to an object.
        /// </summary>
        /// <param name="jsonstring">The JSON string to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        public object DeserializeObject(string jsonstring)
        {
            return JsonConvert.DeserializeObject(jsonstring);
        }
        /// <summary>
        /// Deserializes the JSON string to an object.
        /// </summary>
        /// <param name="filename">The file name for JSON string to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        public List<T> DeserializeObject<T>(string filename)
        {
         //   var settings = new JsonSerializerSettings
         //   {
         ////       TypeNameHandling = TypeNameHandling.All,
         //       NullValueHandling = NullValueHandling.Ignore,
         //       MissingMemberHandling = MissingMemberHandling.Ignore,
         //  //     CheckAdditionalContent = true,
         //       ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
         //       Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }

         //   };

            if (File.Exists(filename))
            {
                String JSONtxt = File.ReadAllText(filename);

                return  JsonConvert.DeserializeObject<List<T>>(JSONtxt, GetSettings());
            }else
            {

                List<T> lists = new List<T>();
                return lists;
            }

        }
        /// <summary>
        /// Deserializes the JSON string to an object.
        /// </summary>
        /// <param name="jsonstring">The JSON string to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        public List<T> DeserializeObjectFromjsonString<T>(string jsonString)
        {
            //var settings = new JsonSerializerSettings
            //{
            ////    TypeNameHandling = TypeNameHandling.All,
            //    NullValueHandling = NullValueHandling.Ignore,
            //    MissingMemberHandling = MissingMemberHandling.Ignore,
            ////    CheckAdditionalContent = true,
            //    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            //    Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }

            //};

            return JsonConvert.DeserializeObject<List<T>>(jsonString, GetSettings());
           

        }
        /// <summary>
        /// Deserializes single Object from string.
        /// </summary>
        /// <param name="jsonstring">The JSON string to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        public T DeserializeSingleObjectFromjsonString<T>(string jsonString)
        {
            //var settings = new JsonSerializerSettings
            //{
            //  //  TypeNameHandling = TypeNameHandling.All,
            //    NullValueHandling = NullValueHandling.Ignore,
            //    MissingMemberHandling = MissingMemberHandling.Ignore,
            //  //  CheckAdditionalContent = true,
            //    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            //    Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }

            //};

            return JsonConvert.DeserializeObject<T>(jsonString, GetSettings());


        }
        /// <summary>
        /// Deserializes single Object from file.
        /// </summary>
        /// <param name="filename">The file that contain JSON string to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        public T DeserializeSingleObject<T>(string filename)
        {
            //var settings = new JsonSerializerSettings
            //{
            //   // TypeNameHandling = TypeNameHandling.All,
            //    NullValueHandling = NullValueHandling.Ignore,
            //    MissingMemberHandling = MissingMemberHandling.Ignore,
            // //   CheckAdditionalContent = true,
            //    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            //    Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }

            //};
            try
            {
                if (File.Exists(filename))
                {
                    String JSONtxt = File.ReadAllText(filename);

                    return JsonConvert.DeserializeObject<T>(JSONtxt, GetSettings());
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
        /// <summary>
        /// Serializes file to object .
        /// </summary>
        /// <param name="filename">The file  to serialize.</param>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>A object.</returns>
        public void Serialize(string filename, object t)
        {
            JsonSerializer serializer = new JsonSerializer() {
                NullValueHandling = NullValueHandling.Include,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                //CheckAdditionalContent=true,
                //TypeNameHandling = TypeNameHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
              
            };
            serializer.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            
            using (StreamWriter file = new StreamWriter(filename))
            {
                try
                {
                    // JsonSerializer serializer = new JsonSerializer();
                    using (JsonWriter writer = new JsonTextWriter(file))
                    {
                        serializer.Serialize(writer, t);
                        // {"ExpiryDate":new Date(1230375600000),"Price":0}
                    }
                   
                }
                catch (Exception ex)
                {

                   Console.WriteLine(ex.ToString());

                }
               
            }
        }
        /// <summary>
        /// Deserializes  Object from file.
        /// </summary>
        /// <param name="stringobject">The JSON string to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        public object DeserializeObjectString<T>(string stringobject)
        {
            //var settings = new JsonSerializerSettings
            //{
            //    NullValueHandling = NullValueHandling.Ignore,
            //    MissingMemberHandling = MissingMemberHandling.Ignore,
            //    //CheckAdditionalContent=true,
            //    //TypeNameHandling = TypeNameHandling.All,
            //    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            //    Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter()}
                

            //};
            return JsonConvert.DeserializeObject<object>(stringobject, GetSettings());
        }
        /// <summary>
        /// Convert Json to DataTable.
        /// </summary>
        /// <param name="jsonString">The JSON string to Convert.</param>
        /// <returns>The DataTable object.</returns>
        public DataTable JsonToDataTable(string jsonString)
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
        /// <summary>
        /// Convert Json to DataSet.
        /// </summary>
        /// <param name="jsonString">The JSON string to Convert.</param>
        /// <returns>The DataSet object.</returns>
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
        /// <summary>
        /// Check Json Token Type.
        /// </summary>
        /// <param name="obj">The JSON JToken to Check.</param>
        /// <returns>The string type name .</returns>
        public string CheckJsonType(JToken obj)
        {
          
                return obj.Type.ToString();
           
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~JsonLoader()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        private JsonSerializerSettings GetSettings()

        {
            return new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                //CheckAdditionalContent=true,
                //TypeNameHandling = TypeNameHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }


            };
        }
    }
}
