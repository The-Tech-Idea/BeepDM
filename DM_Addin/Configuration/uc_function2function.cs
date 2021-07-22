using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS;

namespace TheTechIdea.Configuration
{
    public partial class uc_function2function : UserControl,IDM_Addin, IAddinVisSchema
    {
        public uc_function2function()
        {
            InitializeComponent();
        }

        public string AddinName { get; set; } = "Function to Function Mapping";
        public string Description { get; set; } = "Allow menu class and proces to interconnect though Function to Function Mapping";
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "UserControl";
        public string ParentName { get; set; }
        public bool DefaultCreate { get; set; } = true;
        public string DllPath { get ; set ; }
        public string DllName { get ; set ; }
        public string NameSpace { get ; set ; }
        public DataSet Dset { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public EntityStructure EntityStructure { get ; set ; }
        public string EntityName { get ; set ; }
        public IPassedArgs Passedarg { get ; set ; }
        public IVisUtil Visutil { get; set; }
       // public event EventHandler<PassedArgs> OnObjectSelected;
        #region "IAddinVisSchema"
        public string RootNodeName { get; set; } = "Configuration";
        public string CatgoryName { get; set; }
        public int Order { get; set; } = 8;
        public int ID { get; set; } = 8;
        public string BranchText { get; set; } = "Function to Function Mapping";
        public int Level { get; set; }
        public EnumPointType BranchType { get; set; } = EnumPointType.Entity;
        public int BranchID { get; set; } = 1;
        public string IconImageName { get; set; } = "function2function.ico";
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; } = "";
        public string BranchClass { get; set; } = "ADDIN";
      
        #endregion "IAddinVisSchema"
        public void RaiseObjectSelected()
        {
            throw new NotImplementedException();
        }

        public void Run(string param1)
        {
            throw new NotImplementedException();
        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
        {
            Passedarg = e;
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            this.function2FunctionsBindingSource.DataSource = DMEEditor.ConfigEditor.Function2Functions;
            this.function2FunctionsBindingNavigatorSaveItem.Click += Function2FunctionsBindingNavigatorSaveItem_Click;
            this.fromClassComboBox.SelectedValueChanged += FromClassComboBox_SelectedValueChanged;
            this.toClassComboBox.SelectedValueChanged += ToClassComboBox_SelectedValueChanged;
            foreach (var item in DMEEditor.ConfigEditor.BranchesClasses)
            {
                toClassComboBox.Items.Add(item.className);
                fromClassComboBox.Items.Add(item.className);
            }
            foreach (var item in DMEEditor.ConfigEditor.Events)
            {
                eventComboBox.Items.Add(item.EventName);
            }

            this.actionTypeComboBox.Items.Add("Event");
            this.actionTypeComboBox.Items.Add("Function");
            this.actionTypeComboBox.SelectedValueChanged += ActionTypeComboBox_SelectedValueChanged;
        }

        private void ActionTypeComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
           if (this.actionTypeComboBox.Text == "Event")
            {
                eventComboBox.Enabled = true;
                fromMethodComboBox.Enabled = false;
            }
            else
            {
                eventComboBox.Enabled = false;
                fromMethodComboBox.Enabled = true;
            }
        }

        private void ToClassComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            toMethodComboBox.Items.Clear();
            foreach (var item in DMEEditor.ConfigEditor.BranchesClasses.Where(x => x.className == toClassComboBox.Text).FirstOrDefault().Methods)
            {
                toMethodComboBox.Items.Add(item.Caption);
            }
        }

        private void FromClassComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            fromMethodComboBox.Items.Clear();
            foreach (var item in DMEEditor.ConfigEditor.BranchesClasses.Where(x=>x.className==fromClassComboBox.Text).FirstOrDefault().Methods)
            {
                fromMethodComboBox.Items.Add(item.Caption);
            }
        }

        private void Function2FunctionsBindingNavigatorSaveItem_Click(object sender, EventArgs e)
        {
            try

            {

                this.function2FunctionsBindingSource.EndEdit();
                DMEEditor.ConfigEditor.SaveFucntion2Function();
                MessageBox.Show("Function Mapping Saved successfully", "Beep");
             
            }
            catch (Exception ex)
            {

                ErrorObject.Flag = Errors.Failed;
                string errmsg = "Error Saving Function Mapping ";
                ErrorObject.Message = $"{errmsg}:{ex.Message}";
                errmsg = ErrorObject.Message;
                MessageBox.Show(errmsg, "Beep");
                Logger.WriteLog($" {errmsg} :{ex.Message}");
            }
        }
    }
}
