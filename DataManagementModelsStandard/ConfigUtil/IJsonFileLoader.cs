using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace TheTechIdea.Beep.ConfigUtil
{
    public interface IJsonLoader
    {

        List<T> DeserializeObject<T>(string filename);
        void Serialize(string filename,object t);
        T DeserializeSingleObject<T>(string filename);
        object DeserializeObjectString<T>(string stringobject);
        DataTable JsonToDataTable(string jsonString);
        DataSet ConverttoDataset(string jsonString);
        List<T> DeserializeObjectFromjsonString<T>(string jsonString);
        T DeserializeSingleObjectFromjsonString<T>(string jsonString);
        string SerializeObject(object obj);
        object DeserializeObject(string jsonstring);

    }
}
