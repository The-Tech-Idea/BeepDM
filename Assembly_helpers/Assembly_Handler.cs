using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Assembly_helpers
{
    public  class Assembly_Handler
        {
        public Assembly_Handler()
        {


        }
        Assembly ourAssembly;
        public void CreateAssembly(Dictionary<string, string> propertiesToEmit)
        {
            if (ourAssembly == null)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("using System;");
                sb.AppendLine("public class MyClass");
                sb.AppendLine("{");
                sb.AppendLine("  public static void Main()");
                sb.AppendLine("  {");
                sb.AppendLine("  }");
                foreach (var kvp in propertiesToEmit)
                {
                    sb.AppendLine($"  public {kvp.Value} {kvp.Key}" + " { get; set; }");
                }
                sb.AppendLine("  public  MyClass CreateFromDynamic(Dictionary<string, object> sourceItem)");
                sb.AppendLine("  {");
                sb.AppendLine("     MyClass newOne = new MyClass();");
                foreach (var kvp in propertiesToEmit)
                {
                    sb.AppendLine($@"  newOne.{kvp.Key} = sourceItem[""{kvp.Key}""];");
                }
                sb.AppendLine("  return newOne;");
                sb.AppendLine("  }");
                sb.AppendLine("}");

                var tree = CSharpSyntaxTree.ParseText(sb.ToString());
                var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
                var dictsLib = MetadataReference.CreateFromFile(typeof(Dictionary<,>).Assembly.Location);

                var compilation = CSharpCompilation.Create("MyCompilation",
                    syntaxTrees: new[] { tree }, references: new[] { mscorlib, dictsLib });

                //Emit to stream
                var ms = new MemoryStream();
                var emitResult = compilation.Emit(ms);

                //Load into currently running assembly. Normally we'd probably
                //want to do this in an AppDomain
                ourAssembly = Assembly.Load(ms.ToArray());
            }
        }
    }

   
}
