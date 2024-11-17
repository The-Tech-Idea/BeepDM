using System;
using System.Collections.Generic;
using System.ComponentModel;


namespace TheTechIdea.Beep.ConfigUtil   
{
    public interface IErrorsInfo
    {
        Errors Flag { get; set; }
        Exception Ex { get; set; }
        //IDMLogger logger { get; set; }
        string Message { get; set; }
        string Module { get; set; }
        string Fucntion { get; set; }
        List<IErrorsInfo> Errors { get; set; }

        event PropertyChangedEventHandler PropertyChanged;
    }
}