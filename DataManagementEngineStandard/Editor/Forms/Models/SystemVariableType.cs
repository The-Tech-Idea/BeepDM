using System;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Types of system variables that can be set in Oracle Forms simulation
    /// </summary>
    public enum SystemVariableType
    {
        /// <summary>Current system date (without time)</summary>
        SystemDate,
        
        /// <summary>Current system date and time</summary>
        SystemDateTime,
        
        /// <summary>Current system user</summary>
        SystemUser,
        
        /// <summary>Record status (NEW, CHANGED, etc.)</summary>
        RecordStatus
    }
}