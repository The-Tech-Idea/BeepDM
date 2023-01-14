using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Util;

namespace TheTechIdea.Tools
{
    public interface IClassCreator
    {
        IDMEEditor DMEEditor { get; set; }
        string outputFileName { get; set; }
        string outputpath { get; set; }
        string CreateClass(string classname, List<EntityField> flds,string outputpath, string NameSpacestring="TheTechIdea.ProjectClasses");
        string CreateDLL(string dllname, List<EntityStructure> entities, string outputpath, IProgress<PassedArgs> progress, CancellationToken token, string NameSpacestring = "TheTechIdea.ProjectClasses");
        void GenerateCSharpCode(string fileName);
        void CompileClassFromText(string SourceString, string output);
    }
}
