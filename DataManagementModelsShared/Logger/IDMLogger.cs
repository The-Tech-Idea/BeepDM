using System;
using System.ComponentModel;

namespace TheTechIdea.Logger
{
    public interface IDMLogger
    {
        event EventHandler<string> Onevent;
        event PropertyChangedEventHandler PropertyChanged;
        
        void WriteLog(string info);
    }
}