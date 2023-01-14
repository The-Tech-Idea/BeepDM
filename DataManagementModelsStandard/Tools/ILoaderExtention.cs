using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TheTechIdea.Beep;
using TheTechIdea.Util;

namespace TheTechIdea.Tools
{
    public interface ILoaderExtention
    {
    //    List<IDM_Addin> AddIns { get; set; }
    //    List<assemblies_rep> Assemblies { get; set; }
    //    // AppDomain CurrentDomain { get; set; }
    //    List<AssemblyClassDefinition> DataSourcesClasses { get; set; }
       // IDMEEditor DMEEditor { get; set; }
        IErrorsInfo LoadAllAssembly();
        IErrorsInfo Scan(assemblies_rep assembly);
        IErrorsInfo Scan(Assembly assembly);
        IErrorsInfo Scan();
    }
}
