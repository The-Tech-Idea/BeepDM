using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea;

namespace DataManagementModels.Addin
{
    public interface IAddinExtension
    {
        event EventHandler<PassedArgs> CloseParent;

        bool ValidateAddin();
        bool IsValidated { get; set; }
        bool Save();
        bool IsSaved { get; set; }
        bool Delete();
        bool IsDeleted { get; set; }
        bool Update();
        bool IsUpdated { get; set; }
        bool Add();
        bool IsAdded { get; set; }
        bool Cancel();
        bool IsCancelled { get; set; }
        bool Close();
        bool IsClosed { get; set; }
        bool Open();
        bool IsOpened { get; set; }
        bool Execute();
        bool IsExecuted { get; set; }
        

    }
}
