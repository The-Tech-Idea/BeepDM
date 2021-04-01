using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TheTechIdea.DataManagment_Engine.ConfigUtil
{
    public interface IJsonLoader
    {

        List<T> DeserializeObject<T>(string filename);
        void Serialize(string filename,object t);
        T DeserializeSingleObject<T>(string filename);
        object DeserializeObjectString<T>(string stringobject);

    }
}
