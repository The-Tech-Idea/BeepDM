﻿using Tizen.Applications;
using Uno.UI.Runtime.Skia;

namespace BeepUno.Skia.Tizen
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = new TizenHost(() => new BeepUno.App(), args);
            host.Run();
        }
    }
}
