using System;
using System.ComponentModel;

namespace TheTechIdea.Beep.Logger
{
    public interface IDMLogger
    {
        event EventHandler<string> Onevent;
        event PropertyChangedEventHandler PropertyChanged;
        
        void WriteLog(string info);
        void StartLog();
        void StopLog();
        void PauseLog();
    }
}