using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.AppBuilder
{
    public class AppVersion : IAppVersion
    {
        public AppVersion()
        {

        }
        public AppVersion(int currentversion)
        {
            Ver = currentversion;
        }
        public int Ver { get ; set ; }
        public DateTime CreateDate { get ; set ; }
        public AppType? Apptype { get; set; } = null;
        public string OuputFolder { get ; set ; }
        public string GeneratorName { get; set; }
    }
}
