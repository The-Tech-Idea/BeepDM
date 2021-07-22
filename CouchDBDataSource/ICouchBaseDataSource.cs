using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace TheTechIdea.Beep.NOSQL
{
    public interface ICouchBaseDataSource
    {
        BindingList<IBucket> Buckets { get; set; }
    }
    public interface IBucket
    {
        BindingList<string> Collection { get; set; }
    }
}