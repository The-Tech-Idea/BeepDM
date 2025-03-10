﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Addin
{
    public interface IBeepViewModel 
    {
        string GetName();
        string GetDescription();
        string GetVersion();
        string GetAuthor();
        string GetCompany();
        bool ValidateViewModel();
        bool InitializeViewModel();
        bool LoadViewModel();
        bool SaveViewModel();
        bool CloseViewModel();
        bool DisposeViewModel();
        bool QueryViewModel(string[] parameters);
       
    }
}
