﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.DataManagment_Engine.Vis
{
    public interface IWinFormAddin
    {
        IVisUtil Visutil { get; set; }

    }
}