using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Winforms.VIS
{
    public interface IControlEditor
    {
        PassedArgs Args { get; set; }
        EntityStructure ViewCurrentEntity { get; set; }
        EntityStructure TableCurrentEntity { get; set; }
        IDMEEditor DMEEditor { get; set; }
        IErrorsInfo Erinfo { get; set; }
        IDMLogger Logger { get; set; }
        event EventHandler<PassedArgs> ActionNeeded;
        void RaiseEvent(object sender, EventArgs e, Type SenderType);
        IErrorsInfo GenerateConfigurationViewOnControl(ref Panel control, DataTable table, ref BindingSource bindingsource, int width, EntityStructure datahset);
        IErrorsInfo GenerateTableViewOnControl(string tbname, ref Panel control, DataTable table, ref BindingSource bindingsource, int width,  string datasourceid);
        IErrorsInfo GenerateFilterFieldsForEntityOnControl(string tbname, ref Panel control, DataTable table, ref BindingSource bindingsource, int width, EntityStructure datahset);
        IErrorsInfo GenerateTableViewOnControl(string tbname, ref Panel control, DataTable table, ref BindingSource bindingsource, int width, EntityStructure datahset);
        DialogResult InputBox(string title, string promptText, ref string value);
        DialogResult InputBoxYesNo(string title, string promptText);
        void MsgBox(string title, string promptText);
        DialogResult InputComboBox(string title, string promptText, List<string> itvalues, ref string value);
        BindingList<IReportFilter> CreateFilterQueryGrid(string text, List<EntityField> ls, List<string> lsop);
        IErrorsInfo BindDataSourceEnumtoComobo(ComboBox combo);
        IErrorsInfo BindDataSourceEnumtoComobo(DataGridViewComboBoxColumn combo);
    }
}