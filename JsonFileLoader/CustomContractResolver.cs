using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Util
{
    public class CustomContractResolver:DefaultContractResolver
    {
        protected Dictionary<string, string> PropertyMappings { get; set; }
        protected override string ResolvePropertyName(string propertyName)
        {
            return base.ResolvePropertyName(propertyName);
        }
    }
}
