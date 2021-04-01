using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.DataBase;

namespace TheTechIdea.Tools
{
    public interface IClassCreator
    {
        string outputFileName { get; set; }
        string outputpath { get; set; }
        string CreateClass(string classname, List<EntityField> flds,string classpath, string NameSpacestring="TheTechIdea.ProjectClasses");
        void GenerateCSharpCode(string fileName);
        void CompileClassFromText(string SourceString, string output);
    }
}
