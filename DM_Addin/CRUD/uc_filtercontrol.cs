using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.DataManagment_Engine.Vis;
using WinFormSharedProject.Controls;

namespace TheTechIdea.DataManagment_Engine.AppBuilder.UserControls
{
    public partial class uc_filtercontrol : UserControl,IDM_Addin
    {
        public uc_filtercontrol()
        {
            InitializeComponent();
        }

        public string ParentName { get ; set ; }
        public string ObjectName { get ; set ; } = "Entity Data Filter";
        public string ObjectType { get; set; } = "UserControl";
        public string AddinName { get ; set ; } = "Entity Data Filter";
        public string Description { get ; set ; } = "Entity Data Filter";
        public bool DefaultCreate { get; set; } = false;
        public string DllPath { get ; set ; }
        public string DllName { get ; set ; }
        public string NameSpace { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public EntityStructure EntityStructure { get ; set ; }
        public string EntityName { get ; set ; }
        public PassedArgs Passedarg { get ; set ; }
        IVisUtil Visutil { get; set; }
        IDataSource ds;
        Type enttype;
        object ob;
        string DisplayField;
       // List<FilterType> lsop;
        BindingSource[] BindingData;
        List<DefaultValue> defaults;
        public void Run(string param1)
        {
            throw new NotImplementedException();
        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, PassedArgs e, IErrorsInfo per)
        {
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
            Visutil = (IVisUtil)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            ds = DMEEditor.GetDataSource(e.DatasourceName);
            ds.Dataconnection.OpenConnection();
            if (ds != null && ds.ConnectionStatus == ConnectionState.Open)
            {
                EntityName = e.CurrentEntity;
                EntityStructure = ds.GetEntityStructure(EntityName, true);
                EntityStructure.Filters = new List<ReportFilter>();
                enttype = ds.GetEntityType(EntityName);
                if (EntityStructure != null)
                {
                    if (EntityStructure.Fields != null)
                    {
                        if (EntityStructure.Fields.Count > 0)
                        {
                            // lsop = new List<FilterType>();
                            AddFilterTypes();
                            defaults = DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(i => i.ConnectionName == ds.DatasourceName)].DatasourceDefaults;
                            //CreateControls(this,  EntityStructure, defaults);
                            Visutil.controlEditor.CreateEntityFilterControls(this, EntityStructure, defaults);
                        }
                    }
                }
            }
        }
        private List<FilterType> AddFilterTypes()
        {
            //{ null, "=", ">=", "<=", ">", "<", "Like", "Between" }
            List<FilterType> lsop = new List<FilterType>();
            FilterType filterType = new FilterType("");
            lsop.Add(filterType);

             filterType = new FilterType("=");
            lsop.Add(filterType);

             filterType = new FilterType(">=");
            lsop.Add(filterType);

             filterType = new FilterType("<=");
            lsop.Add(filterType);
             filterType = new FilterType(">");
            lsop.Add(filterType);

             filterType = new FilterType("<");
            lsop.Add(filterType);

             filterType = new FilterType("like");
            lsop.Add(filterType);

            filterType = new FilterType("Between");
            lsop.Add(filterType);
            return lsop;

        }
        private void CreateControls(Control panel, EntityStructure entityStructure,List<DefaultValue> dsdefaults)
        {
            BindingData = new BindingSource[entityStructure.Fields.Count-1];
            int maxlabelsize = 0;
            int maxDatasize = 0;
            foreach (EntityField col in entityStructure.Fields)
            {
                int x = getTextSize(col.fieldname);
                if (maxlabelsize < x)
                    maxlabelsize = x;
            }
            maxDatasize = this.Width - maxlabelsize - 20;
            entityStructure.Filters.Clear();
            List<string> FieldNames = new List<string>();
            var starth = 25;
            int startleft= maxlabelsize + 90;
            int valuewidth = 100;
            for (int i = 0; i < entityStructure.Fields.Count - 1; i++)
            {
                ReportFilter r = new ReportFilter();
                r.FieldName = entityStructure.Fields[i].fieldname;
                r.Operator = null;
                r.FilterValue = null;
                r.FilterValue1 = null;
                r.valueType = entityStructure.Fields[i].fieldtype;
                entityStructure.Filters.Add(r);
                BindingData[i] = new BindingSource();
                BindingData[i].DataSource = r;
                FieldNames.Add(entityStructure.Fields[i].fieldname);
                EntityField col = entityStructure.Fields[i];
                try
                {
                    DefaultValue coldefaults = dsdefaults.Where(o => o.propertyName == col.fieldname).FirstOrDefault();
                    if (coldefaults == null)
                    {
                        coldefaults = dsdefaults.Where(o => col.fieldname.Contains(o.propertyName)).FirstOrDefault();
                    }
                    string coltype = col.fieldtype;
                    RelationShipKeys FK = entityStructure.Relations.Where(f => f.EntityColumnID == col.fieldname).FirstOrDefault();
                    //----------------------
                    Label l = new Label
                    {
                        Top = starth,
                        Left = 10,
                        AutoSize = false,
                        BorderStyle = BorderStyle.FixedSingle,
                        Text = col.fieldname,
                        BackColor = Color.White,
                        ForeColor = Color.Red

                    };
                    l.Size = TextRenderer.MeasureText(col.fieldname, l.Font);
                    l.Height += 10;
                    l.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                    l.Width = maxlabelsize;
                    //---------------------
                    ComboBox cbcondition = new ComboBox
                    {
                        Left = l.Left + l.Width + 10,
                        Top = starth
                    };
                    //string DisplayField = FK.EntityColumnID;
                    //cbcondition.DataSource = Visutil.controlEditor.GetDisplayLookup(ds.DatasourceName, FK.ParentEntityID, FK.ParentEntityColumnID);
                    //cbcondition.DisplayMember = DisplayField;
                    //cbcondition.ValueMember = FK.ParentEntityColumnID;
                    cbcondition.DataSource = AddFilterTypes();
                    cbcondition.DisplayMember = "FilterDisplay";
                    cbcondition.ValueMember = "FilterValue";
                    cbcondition.SelectedValueChanged += Cb_SelectedValueChanged;

                    cbcondition.Width = 50;
                    cbcondition.Height = l.Height;
                   // cbcondition.SelectedText
                    cbcondition.DataBindings.Add(new System.Windows.Forms.Binding("SelectedValue", BindingData[i], "Operator", true, DataSourceUpdateMode.OnPropertyChanged));
                  //  cbcondition.Anchor = AnchorStyles.Top;
                    this.Controls.Add(cbcondition);
                    
                    if (FK != null)
                    {
                        ComboBox cb = new ComboBox
                        {
                            Left = startleft,
                            Top = starth
                        };
                        string DisplayField = FK.EntityColumnID;
                        cb.DataSource = Visutil.controlEditor.GetDisplayLookup(ds.DatasourceName, FK.ParentEntityID, FK.ParentEntityColumnID);
                        cb.DisplayMember = DisplayField;
                        cb.ValueMember = FK.ParentEntityColumnID;
                        cb.Width = valuewidth;
                        cb.Height = l.Height;
                        cb.DataBindings.Add(new System.Windows.Forms.Binding("TEXT", BindingData[i], "FilterValue", true));
                       cb.SelectedValueChanged += Cb_SelectedValueChanged;
                        //cb.Anchor = AnchorStyles.Top;
                        this.Controls.Add(cb);

                        starth = l.Bottom + 1;
                    }
                    else
                    {
                        switch (coltype)
                        {
                            case "System.DateTime":
                                DateTimePicker dt = new DateTimePicker
                                {
                                    Left = startleft,
                                    Top = starth
                                };
                                dt.DataBindings.Add(new System.Windows.Forms.Binding("Value", BindingData[i], "FilterValue", true));

                                dt.Width = valuewidth;
                                dt.Height = l.Height;
                                dt.ValueChanged += Dt_ValueChanged;
                           //     dt.Anchor = AnchorStyles.Top;
                                dt.Tag = i;
                                dt.Format = DateTimePickerFormat.Short;
                                this.Controls.Add(dt);
                                DateTimePicker dt1 = new DateTimePicker
                                {
                                    Left = dt.Left+10+dt.Width,
                                    Top = starth
                                };
                                dt1.DataBindings.Add(new System.Windows.Forms.Binding("Value", BindingData[i], "FilterValue1", true));

                                dt1.Width = valuewidth;
                                dt1.Height = l.Height;
                                dt1.Format = DateTimePickerFormat.Short;
                                 dt1.ValueChanged += Dt_ValueChanged;
                                //     dt.Anchor = AnchorStyles.Top;
                                dt1.Tag = i;
                                this.Controls.Add(dt1);
                                break;
                            case "System.TimeSpan":
                                TextBox t1 = new TextBox
                                {
                                    Left = startleft,
                                    Top = starth
                                };

                                t1.DataBindings.Add(new System.Windows.Forms.Binding("Text", BindingData[i], "FilterValue", true));
                                t1.TextAlign = HorizontalAlignment.Left;
                                t1.Width = valuewidth;
                                t1.Height = l.Height;

                                t1.Tag = i;
                                   t1.TextChanged += T_TextChanged;
                                //  t1.KeyPress += T_KeyPress;
                           //     t1.Anchor = AnchorStyles.Top;
                                this.Controls.Add(t1);
                                break;
                            case "System.Boolean":
                                CheckBox ch1 = new CheckBox
                                {
                                    Left = startleft,
                                    Top = starth
                                };

                                ch1.DataBindings.Add(new System.Windows.Forms.Binding("CheckState", BindingData[i], "FilterValue", true));
                                ch1.Text = "";
                                ch1.Width = valuewidth;
                                ch1.Height = l.Height;
                                ch1.CheckStateChanged += Ch1_CheckStateChanged; ;
                         //       ch1.Anchor = AnchorStyles.Top;
                                ch1.Tag = i;
                                this.Controls.Add(ch1);


                                break;
                            case "System.Char":
                                MyCheckBox ch2 = new MyCheckBox
                                {
                                    Left = startleft,
                                    Top = starth
                                };

                                ch2.DataBindings.Add(new System.Windows.Forms.Binding("Checked", BindingData[i], "FilterValue", true));
                                ch2.Text = "";
                                ch2.Width = valuewidth;
                                ch2.Height = l.Height;
                                string[] v = coldefaults.propoertValue.Split(',');

                                if (coldefaults != null)
                                {
                                    ch2.TrueValue = v[0].ToCharArray()[0];
                                    ch2.FalseValue = v[1].ToCharArray()[0];
                                }
                               ch2.CheckStateChanged += Ch1_CheckStateChanged; ;
                             //   ch2.Anchor = AnchorStyles.Top;
                                ch2.Tag = i;
                                this.Controls.Add(ch2);

                                break;
                            case "System.Int16":
                            case "System.Int32":
                            case "System.Int64":
                            case "System.Decimal":
                            case "System.Double":
                            case "System.Single":
                                NumericUpDown t3 = new NumericUpDown();

                                t3.Left = startleft;
                                t3.Top = starth;
                                t3.DataBindings.Add(new System.Windows.Forms.Binding("Value", BindingData[i], "FilterValue", true));
                                t3.TextAlign = HorizontalAlignment.Left;
                                t3.Width = valuewidth;
                                t3.Height = l.Height;
                                t3.Tag = i;
                                t3.TextChanged += T_TextChanged;
                                //t.KeyPress += T_KeyPress;
                                if (entityStructure.PrimaryKeys.Where(x => x.fieldname == col.fieldname).FirstOrDefault() != null)
                                {
                                    t3.Enabled = false;
                                }
                                this.Controls.Add(t3);

                                NumericUpDown t2 = new NumericUpDown();
                                t2.Left = t3.Left+t3.Width+10;
                                t2.Top = starth;
                                t2.DataBindings.Add(new System.Windows.Forms.Binding("Value", BindingData[i], "FilterValue1", true));
                                t2.TextAlign = HorizontalAlignment.Left;
                                t2.Width = valuewidth;
                                t2.Height = l.Height;
                                t2.Tag = i;
                                t2.TextChanged += T_TextChanged;
                                //t.KeyPress += T_KeyPress;
                                if (entityStructure.PrimaryKeys.Where(x => x.fieldname == col.fieldname).FirstOrDefault() != null)
                                {
                                    t2.Enabled = false;
                                }
                                //   t.Anchor = AnchorStyles.Top;

                                this.Controls.Add(t2);
                                //   t.Anchor = AnchorStyles.Top;
                                break;
                               
                            case "System.String":
                                if (entityStructure.Fields.Where(p => p.fieldname == col.fieldname).FirstOrDefault().Size1 > 1)
                                {
                                     t1 = new TextBox
                                    {
                                        Left = startleft,
                                        Top = starth
                                    };

                                    t1.DataBindings.Add(new System.Windows.Forms.Binding("Text", BindingData[i], "FilterValue", true));
                                    t1.TextAlign = HorizontalAlignment.Left;
                                    t1.Width = valuewidth;
                                    t1.Height = l.Height;

                                    t1.TextChanged += T_TextChanged;
                                  //  t1.KeyPress += T_KeyPress;
                                    if (entityStructure.PrimaryKeys.Any(x => x.fieldname == col.fieldname))
                                    {
                                        if (entityStructure.Relations.Any(x => x.EntityColumnID == col.fieldname))
                                        {
                                            t1.Enabled = false;
                                        }

                                    }
                                    this.Controls.Add(t1);
                                   t1.Tag = i;
                              //      t1.Anchor = AnchorStyles.Top;
                                }
                                else
                                {
                                     ch2 = new MyCheckBox
                                    {
                                        Left = startleft,
                                        Top = starth
                                    };

                                    ch2.DataBindings.Add(new System.Windows.Forms.Binding("Checked", BindingData[i], "FilterValue", true));
                                    ch2.Text = "";
                                    ch2.Width = valuewidth;
                                    ch2.Height = l.Height;
                                    ch2.Tag = i;


                                    if (coldefaults != null)
                                    {
                                        v = coldefaults.propoertValue.Split(',');
                                        ch2.TrueValue = v[0].ToCharArray()[0];
                                        ch2.FalseValue = v[1].ToCharArray()[0];
                                    }
                                   // ch2.CheckStateChanged += Ch1_CheckStateChanged; ;

                                    this.Controls.Add(ch2);

                               //     ch2.Anchor = AnchorStyles.Top;
                                }
                                break;
                            default:
                                TextBox t = new TextBox();

                                t.Left = startleft;
                                t.Top = starth;
                                t.DataBindings.Add(new System.Windows.Forms.Binding("Text", BindingData[i], "FilterValue", true));
                                t.TextAlign = HorizontalAlignment.Left;
                                t.Width = valuewidth;
                                t.Height = l.Height;
                                t.Tag = i;
                                t.TextChanged += T_TextChanged;
                                //t.KeyPress += T_KeyPress;
                                if (entityStructure.PrimaryKeys.Where(x => x.fieldname == col.fieldname).FirstOrDefault() != null)
                                {
                                    t.Enabled = false;
                                }
                             //   t.Anchor = AnchorStyles.Top;

                                this.Controls.Add(t);

                                break;

                        }
                    }
                   // l.Anchor = AnchorStyles.Top;
                    this.Controls.Add(l);
                    starth = l.Bottom + 1;

                }
                catch (Exception ex)
                {

                    Logger.WriteLog($"Error in Loading View ({ex.Message}) ");
                }

            }

       
        }
        public object GetDisplayLookup(string datasourceid, string entityname, string KeyField)
        {
            object retval;
            try
            {
                IDataSource ds = DMEEditor.GetDataSource(datasourceid);
                EntityStructure ent = ds.GetEntityStructure(entityname, false);
                DisplayField = null;
                // bool found = true;
                // int i = 0;
                List<DefaultValue> defaults = ds.Dataconnection.ConnectionProp.DatasourceDefaults.Where(o => o.propertyType == DefaultValueType.DisplayLookup).ToList();
                List<string> fields = ent.Fields.Select(u => u.EntityName).ToList();
                if (defaults != null)
                {

                    DisplayField = (from x in ent.Fields
                                    join y in defaults on x.fieldname equals y.propertyName
                                    orderby y.propoertValue
                                    select x.fieldname).FirstOrDefault();

                }
                if (string.IsNullOrWhiteSpace(DisplayField) || string.IsNullOrEmpty(DisplayField))
                {
                    DisplayField = ent.Fields.Where(r => r.EntityName.Contains("NAME")).Select(u => u.EntityName).FirstOrDefault();
                }
                if (string.IsNullOrWhiteSpace(DisplayField) || string.IsNullOrEmpty(DisplayField))
                {
                    DisplayField = KeyField;
                }
                string qrystr = "select " + DisplayField + "," + KeyField + " from " + entityname;



                retval = ds.RunQuery(qrystr);

                return retval;
            }
            catch (Exception)
            {

                return null;
            }
        }
        private int getTextSize(string text)
        {
            Font font = new Font("Courier New", 10.0F);
            Image fakeImage = new Bitmap(1, 1);
            Graphics graphics = Graphics.FromImage(fakeImage);
            SizeF size = graphics.MeasureString(text, font);
            return Convert.ToInt32(size.Width);
        }
        #region "Detect Changes with Dynamiclly generated userControl"
        private void SetFilterPropertyValue(string Fieldname, string Value)
        {
            ReportFilter ent = EntityStructure.Filters.Where(x => x.FieldName == Fieldname).FirstOrDefault();
            if (ent == null)
            {
                ent = new ReportFilter();
                ent.FieldName = Fieldname;
                ent.FilterValue = Value;
                EntityStructure.Filters.Add(ent);
            }
            else
            {
                ent.FilterValue = Value;
            }

        }
        private void T_KeyPress(object sender, KeyPressEventArgs e)
        {
            // TextBox tx=(TextBox) sender;
            if (char.IsControl(e.KeyChar)) //&& !char.IsDigit(e.KeyChar)'  && (e.KeyChar != '.'
            {
                e.Handled = true;
            }
            //else
            //{
            //    SetFilterPropertyValue(tx.Name, tx.Text);
            //}
        }
        private void T_TextChanged(object sender, EventArgs e)
        {
            var tx = (TextBox)sender;
            if (System.Text.RegularExpressions.Regex.IsMatch(tx.Text, "  ^ [0-9]"))
            {
                tx.Text = null;
            }
            else
            {
                SetFilterPropertyValue(tx.Name, tx.Text);
            }
            RaiseEvent(sender, e, typeof(TextBox));


        }
        public void RaiseEvent(object sender, EventArgs e, Type SenderType)
        {
            Control tx = null;
            switch (SenderType.Name)
            {
                case "TextBox":

                    tx = (TextBox)sender;
                    break;
                case "ComboBox":

                    tx = (ComboBox)sender;
                    break;
                case "CheckBox":

                    tx = (CheckBox)sender;
                    break;
                case "DateTimePicker":

                    tx = (DateTimePicker)sender;
                    break;
                default:
                    break;
            }

            string[] args = { "TextBox", tx.Name, tx.Text };
            List<ObjectItem> ob = new List<ObjectItem>(); ;
            ObjectItem it = new ObjectItem();
            it.obj = tx;
            it.Name = "TextBox";
            ob.Add(it);

            PassedArgs Passedarguments = new PassedArgs
            {
                Addin = null,
                AddinName = null,
                AddinType = "",
                DMView = null,
                CurrentEntity = tx.Name,
                ObjectType = "TEXTBOXCHANGED",
                DataSource = null,
                ObjectName = tx.Name,
                Objects = ob,
                EventType = "TEXTBOXCHANGED"

            };
           // ActionNeeded?.Invoke(this, Passedarguments);
        }
        private void Cb_SelectedValueChanged(object sender, EventArgs e)
        {
            // throw new NotImplementedException();
        }
        private void Dt_ValueChanged(object sender, EventArgs e)
        {
            // throw new NotImplementedException();
        }
        private void Ch1_CheckStateChanged(object sender, EventArgs e)
        {
            // throw new NotImplementedException();
        }
        #endregion "Detect Changes with Dynamiclly generated userControl"
    }
}
