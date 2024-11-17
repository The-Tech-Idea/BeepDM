using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.Addin
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ImporterExporterAttr: Attribute
   {
        public string ImporterExporterName { get; set; }
        public string ImporterExporterType { get; set; }
        public string ImporterExporterDescription { get; set; }
        public string ImporterExporterVersion { get; set; }
        public string ImporterExporterAuthor { get; set; }
        public string ImporterExporterAuthorContact { get; set; }
        public string ImporterExporterAuthorUrl { get; set; }
        public string extenssions { get; set; }
        
    }
}
