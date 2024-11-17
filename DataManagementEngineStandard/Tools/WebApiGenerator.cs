
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
namespace TheTechIdea.Beep.Tools
{
    public class WebApiGenerator
    {
        public WebApiGenerator()
        {

        }
        public IDMEEditor DMEEditor { get; set; }
        public IDataSource dataSource { get; set; }
        public string CreateWebAPI(string classname, List<EntityStructure> propertiesToEmit)
        {
            string retval="";

            return retval;
        }
        public string CreateModel(string classname, List<EntityStructure> propertiesToEmit)
        {
            string retval = "";

            return retval;
        }
        public string CreateController(string projectname,string modelname,List<EntityStructure> propertiesToEmit,string classname)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic; ");
            sb.AppendLine("using System.Linq; ");
            sb.AppendLine("using System.Net; ");
            sb.AppendLine("using System.Net.Http; ");
            sb.AppendLine( "using System.Web.Http; ");
            sb.AppendLine($"namespace {projectname}.Controllers");
            sb.AppendLine("{");
            sb.AppendLine($"public class {modelname}Controller : ApiController");
            sb.AppendLine("{");
            sb.AppendLine($"public IEnumerable<{modelname}> {modelname}");
            sb.AppendLine($"// GET api/{modelname}");
            sb.AppendLine($"public IEnumerable<{modelname}> Get()");
            sb.AppendLine("{");
            sb.AppendLine("return new ;");
            sb.AppendLine("}");
            sb.AppendLine($"// GET api/{modelname}/5");
            sb.AppendLine($"public {classname} Get(int id)");
            sb.AppendLine("{");
            sb.AppendLine("return value;");
            sb.AppendLine("}");
            sb.AppendLine($"// POST api/{modelname}");
            sb.AppendLine($"public void Post([FromBody] {modelname} value)");
            sb.AppendLine("{    ");
            sb.AppendLine("}");
            sb.AppendLine($"// PUT api/{modelname}/5");
            sb.AppendLine($"public void Put(int id, [FromBody] {modelname} value)");
            sb.AppendLine("{    ");
            sb.AppendLine("}");
            sb.AppendLine($"// DELETE api/modelname/5");
            sb.AppendLine("public void Delete(int id)");
            sb.AppendLine("{    ");
            sb.AppendLine("}");
            sb.AppendLine("}");
            sb.AppendLine("}");
            return sb.ToString();
                //var tree = CSharpSyntaxTree.ParseText(sb.ToString());
                //var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
                //var dictsLib = MetadataReference.CreateFromFile(typeof(Dictionary<,>).Assembly.Location);

                //var compilation = CSharpCompilation.Create("MyCompilation",
                 //   syntaxTrees: new[] { tree }, references: new[] { mscorlib, dictsLib });

                //Emit to stream
               // var ms = new MemoryStream();
               // var emitResult = compilation.Emit(ms);

                //Load into currently running assembly. Normally we'd probably
                //want to do this in an AppDomain
               // ourAssembly = Assembly.Load(ms.ToArray());
           
        }
    }
}
