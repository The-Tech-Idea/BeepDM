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
using TheTechIdea.Beep.Report;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Winforms.VIS.ReportGenrerator;
using WinFormSharedProject.Controls;
using static TheTechIdea.Winforms.VIS.VisUtil;

namespace TheTechIdea.Winforms.VIS
{
    public class ControlEditor : IControlEditor
    {
        public event EventHandler<PassedArgs> ActionNeeded;
        public ControlEditor(IDMEEditor pDMEEditor, IDMLogger logger, IErrorsInfo per)
        {
            DMEEditor = pDMEEditor;
            Logger = logger;

            Erinfo = per;
        }
        public IDMLogger Logger { get; set; }
        public IErrorsInfo Erinfo { get; set; }
        public PassedArgs Args { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public EntityStructure ViewCurrentEntity { get; set; }
        public EntityStructure TableCurrentEntity { get; set; }
        public Dictionary<string, Control> controls { get; set; } = new Dictionary<string, Control>();
        string DisplayField;
        #region "Create Controls Dynamiclly"
        #region "Detect Changes with Dynamiclly generated userControl"
        private void SetFilterPropertyValue(string Fieldname, string Value)
        {
            ReportFilter ent = TableCurrentEntity.Filters.Where(x => x.FieldName == Fieldname).FirstOrDefault();
            if (ent == null)
            {
                ent = new ReportFilter();
                ent.FieldName = Fieldname;
                ent.FilterValue = Value;
                TableCurrentEntity.Filters.Add(ent);
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
                tx.Text =null;
            }
            //else
            //{
            // //   SetFilterPropertyValue(tx.Name, tx.Text);
            //}
            RaiseEvent(sender, e, typeof(TextBox));


        }
        public void RaiseEvent(object sender, EventArgs e,Type SenderType)
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
            ActionNeeded?.Invoke(this, Passedarguments);
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
        public object GetDisplayLookup(string datasourceid,string entityname,string KeyField)
        {
            object retval;
            try
            {
                IDataSource ds = DMEEditor.GetDataSource(datasourceid);
                EntityStructure ent = ds.GetEntityStructure(entityname,false);
                 DisplayField = null;
               // bool found = true;
               // int i = 0;
                List<DefaultValue> defaults = ds.Dataconnection.ConnectionProp.DatasourceDefaults.Where(o=>o.propertyType==DefaultValueType.DisplayLookup).ToList();
                List<string> fields = ent.Fields.Select(u => u.EntityName).ToList();
                if (defaults != null)
                {

                     DisplayField = (from x in ent.Fields
                                           join y in defaults on x.fieldname  equals y.propertyName  orderby y.propoertValue select x.fieldname).FirstOrDefault();

                }
                if (string.IsNullOrWhiteSpace(DisplayField) || string.IsNullOrEmpty(DisplayField))
                {
                    DisplayField= ent.Fields.Where(r=>r.EntityName.Contains("NAME")).Select(u => u.EntityName ).FirstOrDefault();
                }
                if (string.IsNullOrWhiteSpace(DisplayField) || string.IsNullOrEmpty(DisplayField))
                {
                    DisplayField = KeyField;
                }
                string qrystr = "select " + DisplayField + "," + KeyField + " from " + entityname;

              
                retval = ds.RunQuery(qrystr );
               
                return retval;
            }
            catch (Exception )
            {

                return null;
            }
        }
        #endregion "Detect Changes with Dynamiclly generated userControl"
        #region "Table Events"
        private void Table_RowDeleted(object sender, DataRowChangeEventArgs e)
        {
           // throw new NotImplementedException();
        }

        private void Table_ColumnChanged(object sender, DataColumnChangeEventArgs e)
        {
           // throw new NotImplementedException();
        }

        private void Table_RowChanged(object sender, DataRowChangeEventArgs e)
        {
            DataRow item = e.Row;
           
            switch (e.Action)
            {
                case DataRowAction.Delete:
                    break;
                case DataRowAction.Change:
                    break;
                  
                case DataRowAction.Add:
                    
                default:
                    break;
            }
        }

        private void Table_TableNewRow(object sender, DataTableNewRowEventArgs e)
        {
          
            // throw new NotImplementedException();
        }
        #endregion
        #region "BindingSource Events"

        private void Bindingsource_ListChanged(object sender, ListChangedEventArgs e)
        {
           // throw new NotImplementedException();
        }

        private void Bindingsource_PositionChanged(object sender, EventArgs e)
        {
            
        }

        private void Bindingsource_CurrentChanged(object sender, EventArgs e)
        {
           // throw new NotImplementedException();
        }


        #endregion
        public int getTextSize(string text)
        {
            Font font = new Font("Courier New", 10.0F);
            Image fakeImage = new Bitmap(1, 1);
            Graphics graphics = Graphics.FromImage(fakeImage);
            SizeF size = graphics.MeasureString(text, font);
            return Convert.ToInt32(size.Width);
        }
       
        //----------------------------------------------------------
        public IErrorsInfo GenerateTableViewOnControl(string tbname, ref Panel control, DataTable table, ref BindingSource bindingsource, int width, EntityStructure datahset)
        {

            GenerateTableViewOnControl(datahset.EntityName, ref control, table, ref bindingsource, 200,  datahset.DataSourceID);
            return Erinfo;
        }
        public IErrorsInfo GenerateTableViewOnControl(string entityname, ref Panel control, DataTable table, ref BindingSource bindingsource, int width, string datasourceid)
        {
            controls = new Dictionary<string, Control>();
            Erinfo.Flag = Errors.Ok;
            TextBox t1 = new TextBox();
            
            if (table == null)
            {
                MessageBox.Show("Error", " Table has no Data");
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message= "Error Table has no Data";
                return DMEEditor.ErrorObject;
            }
            IDataSource ds = DMEEditor.GetDataSource(datasourceid);
            List<DefaultValue> defaults = DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(i => i.ConnectionName == ds.DatasourceName)].DatasourceDefaults;
            TableCurrentEntity = ds.GetEntityStructure(entityname,false);
            table.TableNewRow += Table_TableNewRow;
            table.RowChanged += Table_RowChanged;
            table.ColumnChanged += Table_ColumnChanged;
            table.RowDeleted += Table_RowDeleted;
          
            bindingsource.CurrentChanged += Bindingsource_CurrentChanged;
            bindingsource.PositionChanged += Bindingsource_PositionChanged;
            bindingsource.ListChanged += Bindingsource_ListChanged;
            // Create Filter Control
            // CreateFilterQueryGrid(entityname, TableCurrentEntity.Fields, null);
            try
            {
                var starth = 25;
               
                TableCurrentEntity.Filters = new List<Beep.Report.ReportFilter>();
                foreach (DataColumn col in table.Columns)
                {
                    DefaultValue coldefaults = defaults.Where(o => o.propertyName == col.ColumnName).FirstOrDefault();
                    if (coldefaults == null)
                    {
                        coldefaults = defaults.Where(o => col.ColumnName.Contains(o.propertyName)).FirstOrDefault();
                    }
                    string coltype = col.DataType.Name.ToString();
                    RelationShipKeys FK = TableCurrentEntity.Relations.Where(f => f.EntityColumnID == col.ColumnName).FirstOrDefault();
                    //----------------------
                    Label l = new Label
                    {
                        Top = starth,
                        Left = 10,
                        AutoSize = false,
                        BorderStyle = BorderStyle.FixedSingle,
                        Text = col.ColumnName,
                        BackColor = Color.White,
                        ForeColor=Color.Red

                    };
                    l.Size = TextRenderer.MeasureText(col.ColumnName, l.Font);
                    l.Height += 10;
                    l.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                    l.Width = width;
                    //---------------------

                    if (FK != null)
                    {
                        ComboBox cb = new ComboBox
                        {
                            Left = l.Left + l.Width + 10,
                            Top = starth
                        };
                         DisplayField = FK.EntityColumnID;
                        cb.DataSource = GetDisplayLookup(datasourceid, FK.ParentEntityID, FK.ParentEntityColumnID); 
                        cb.DisplayMember = DisplayField;
                        cb.ValueMember = FK.ParentEntityColumnID;
                        cb.Width = width;
                        cb.Height = l.Height;
                        cb.DataBindings.Add(new System.Windows.Forms.Binding("TEXT", bindingsource, col.ColumnName, true));
                        cb.SelectedValueChanged += Cb_SelectedValueChanged;
                        cb.Anchor = AnchorStyles.Top;
                        control.Controls.Add(cb);
                        controls.Add(col.ColumnName, cb);
                        starth = l.Bottom + 1;
                    }
                    else
                    {
                        switch (coltype)
                        {
                            case "DateTime":
                                DateTimePicker dt = new DateTimePicker
                                {
                                    Left = l.Left + l.Width + 10,
                                    Top = starth
                                };
                                dt.DataBindings.Add(new System.Windows.Forms.Binding("Value", bindingsource, col.ColumnName, true));

                                dt.Width = width;
                                dt.Height = l.Height;
                                dt.ValueChanged += Dt_ValueChanged;
                                dt.Anchor = AnchorStyles.Top;
                                control.Controls.Add(dt);
                                controls.Add(col.ColumnName,dt);
                                break;
                            case "TimeSpan":
                                t1 = new TextBox
                                {
                                    Left = l.Left + l.Width + 10,
                                    Top = starth
                                };

                                t1.DataBindings.Add(new System.Windows.Forms.Binding("Text", bindingsource, col.ColumnName, true));
                                t1.TextAlign = HorizontalAlignment.Left;
                                t1.Width = width;
                                t1.Height = l.Height;
                                control.Controls.Add(t1);
                                controls.Add(col.ColumnName, t1);
                                t1.TextChanged += T_TextChanged;
                                t1.KeyPress += T_KeyPress;
                                t1.Anchor = AnchorStyles.Top;
                                break;
                            case "Boolean":
                                CheckBox ch1 = new CheckBox
                                {
                                    Left = l.Left + l.Width + 10,
                                    Top = starth
                                };

                                ch1.DataBindings.Add(new System.Windows.Forms.Binding("CheckState", bindingsource, col.ColumnName, true));
                                ch1.Text = "";
                                ch1.Width = width;
                                ch1.Height = l.Height;
                                ch1.CheckStateChanged += Ch1_CheckStateChanged; ;
                                ch1.Anchor = AnchorStyles.Top;
                                control.Controls.Add(ch1);
                                controls.Add(col.ColumnName, ch1);

                                break;
                            case "Char":
                                MyCheckBox ch2 = new MyCheckBox
                                {
                                    Left = l.Left + l.Width + 10,
                                    Top = starth
                                };

                                ch2.DataBindings.Add(new System.Windows.Forms.Binding("Checked", bindingsource, col.ColumnName, true));
                                ch2.Text = "";
                                ch2.Width = width;
                                ch2.Height = l.Height;
                                string[] v=coldefaults.propoertValue.Split(',');

                                if (coldefaults != null)
                                {
                                    ch2.TrueValue = v[0].ToCharArray()[0];
                                    ch2.FalseValue = v[1].ToCharArray()[0];
                                }
                                ch2.CheckStateChanged += Ch1_CheckStateChanged; ;
                                ch2.Anchor = AnchorStyles.Top;
                                control.Controls.Add(ch2);
                                controls.Add(col.ColumnName, ch2);
                                break;
                            case "Int16":
                            case "Int32":
                            case "Int64":
                            case "Decimal":
                            case "Double":
                            case "Single":
                            case "String":
                                if (TableCurrentEntity.Fields.Where(p=>p.fieldname==col.ColumnName).FirstOrDefault().Size1 > 1)
                                {
                                    t1 = new TextBox
                                    {
                                        Left = l.Left + l.Width + 10,
                                        Top = starth
                                    };

                                    t1.DataBindings.Add(new System.Windows.Forms.Binding("Text", bindingsource, col.ColumnName, true));
                                    t1.TextAlign = HorizontalAlignment.Left;
                                    t1.Width = width;
                                    t1.Height = l.Height;
                                    control.Controls.Add(t1);
                                    controls.Add(col.ColumnName, t1);
                                    t1.TextChanged += T_TextChanged;
                                    t1.KeyPress += T_KeyPress;
                                    if (TableCurrentEntity.PrimaryKeys.Any(x => x.fieldname == col.ColumnName))
                                    {
                                        if (TableCurrentEntity.Relations.Any(x => x.EntityColumnID == col.ColumnName))
                                        {
                                            t1.Enabled = false;
                                        }
                                         
                                    }
                                    t1.Anchor = AnchorStyles.Top;
                                }
                                else
                                {
                                     ch2 = new MyCheckBox
                                    {
                                        Left = l.Left + l.Width + 10,
                                        Top = starth
                                    };

                                    ch2.DataBindings.Add(new System.Windows.Forms.Binding("Checked", bindingsource, col.ColumnName, true));
                                    ch2.Text = "";
                                    ch2.Width = width;
                                    ch2.Height = l.Height;
  
                              
                                    
                                    if (coldefaults != null)
                                    {
                                        v = coldefaults.propoertValue.Split(',');
                                        ch2.TrueValue = v[0].ToCharArray()[0];
                                        ch2.FalseValue = v[1].ToCharArray()[0];
                                    }
                                    ch2.CheckStateChanged += Ch1_CheckStateChanged; ;

                                    control.Controls.Add(ch2);
                                    controls.Add(col.ColumnName, ch2);
                                    ch2.Anchor = AnchorStyles.Top;
                                }                       
                                break;
                            default:
                                using (TextBox t = new TextBox())
                                {
                                    t.Left = l.Left + l.Width + 10;
                                    t.Top = starth;
                                    t.DataBindings.Add(new System.Windows.Forms.Binding("Text", bindingsource, col.ColumnName, true));
                                    t.TextAlign = HorizontalAlignment.Left;
                                    t.Width = width;
                                    t.Height = l.Height;
                                    control.Controls.Add(t);
                                    controls.Add(col.ColumnName, t);
                                    t.TextChanged += T_TextChanged;
                                    t.KeyPress += T_KeyPress;
                                    if (TableCurrentEntity.PrimaryKeys.Where(x => x.fieldname == col.ColumnName).FirstOrDefault() != null)
                                    {
                                        t.Enabled = false;
                                    }
                                    t.Anchor = AnchorStyles.Top;
                                }

                                break;

                        }
                    }
                    l.Anchor = AnchorStyles.Top;
                    control.Controls.Add(l);

                    starth = l.Bottom + 1;
                    //this.databaseTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.dataConnectionsBindingSource, "Database", true));

                }

            }
            catch (Exception ex)
            {
                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
                Logger.WriteLog($"Error in Loading View ({ex.Message}) ");
            }
            return Erinfo;
        }
        public object GenerateEntityonControl(string entityname, ref Panel control, object record, ref BindingSource bindingsource, int width, string datasourceid)
        {
            controls = new Dictionary<string, Control>();
            Erinfo.Flag = Errors.Ok;
            TextBox t1 = new TextBox();

            if (record == null)
            {
                MessageBox.Show("Error", " record has no Data");
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Error Table has no Data";
                return DMEEditor.ErrorObject;
            }
           
            IDataSource ds = DMEEditor.GetDataSource(datasourceid);
            List<DefaultValue> defaults = DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(i => i.ConnectionName == ds.DatasourceName)].DatasourceDefaults;
            TableCurrentEntity = ds.GetEntityStructure(entityname, false);
            Type enttype = ds.GetEntityType(entityname);
            var ti=Activator.CreateInstance(enttype);
            // ICustomTypeDescriptor, IEditableObject, IDataErrorInfo, INotifyPropertyChanged
            if (record.GetType().GetInterfaces().Contains(typeof(ICustomTypeDescriptor)))
            {
                DataRowView dv =(DataRowView) record;
                DataRow dr = dv.Row;
                foreach (EntityField col in TableCurrentEntity.Fields)
                {
                   // TrySetProperty<enttype>(ti, dr[col.fieldname], null);
                   if (dr[col.fieldname] != System.DBNull.Value)
                    {
                        System.Reflection.PropertyInfo PropAInfo = enttype.GetProperty(col.fieldname);
                        PropAInfo.SetValue(ti, dr[col.fieldname], null);
                    }
                   
                }


            }else
            {
                ti = record;
            }
            bindingsource.DataSource = ti;
            //bindingsource.CurrentChanged += Bindingsource_CurrentChanged;
            //bindingsource.PositionChanged += Bindingsource_PositionChanged;
            //bindingsource.ListChanged += Bindingsource_ListChanged;
            // Create Filter Control
            // CreateFilterQueryGrid(entityname, TableCurrentEntity.Fields, null);
            //--- Get Max label size
            int maxlabelsize = 0;
            int maxDatasize = 0;
            foreach (EntityField col in TableCurrentEntity.Fields)
            {
                int x= getTextSize(col.fieldname);
                if (maxlabelsize< x)
                    maxlabelsize = x;
            }
            maxDatasize = control.Width - maxlabelsize - 20;
            try
            {
                var starth = 25;

                TableCurrentEntity.Filters = new List<Beep.Report.ReportFilter>();
                foreach (EntityField col in TableCurrentEntity.Fields)
                {
                    DefaultValue coldefaults = defaults.Where(o => o.propertyName == col.fieldname).FirstOrDefault();
                    if (coldefaults == null)
                    {
                        coldefaults = defaults.Where(o => col.fieldname.Contains(o.propertyName)).FirstOrDefault();
                    }
                    string coltype = col.fieldtype;
                    RelationShipKeys FK = TableCurrentEntity.Relations.Where(f => f.EntityColumnID == col.fieldname).FirstOrDefault();
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

                    if (FK != null)
                    {
                        ComboBox cb = new ComboBox
                        {
                            Left = l.Left + l.Width + 10,
                            Top = starth
                        };
                        DisplayField = FK.EntityColumnID;
                        cb.DataSource = GetDisplayLookup(datasourceid, FK.ParentEntityID, FK.ParentEntityColumnID);
                        cb.DisplayMember = DisplayField;
                        cb.ValueMember = FK.ParentEntityColumnID;
                        cb.Width = maxDatasize;
                        cb.Height = l.Height;
                        cb.DataBindings.Add(new System.Windows.Forms.Binding("TEXT", bindingsource, col.fieldname, true));
                        cb.SelectedValueChanged += Cb_SelectedValueChanged;
                        cb.Anchor = AnchorStyles.Top;
                        control.Controls.Add(cb);
                        controls.Add(col.fieldname, cb);
                        starth = l.Bottom + 1;
                    }
                    else
                    {
                        switch (coltype)
                        {
                            case "System.DateTime":
                                DateTimePicker dt = new DateTimePicker
                                {
                                    Left = l.Left + l.Width + 10,
                                    Top = starth
                                };
                                dt.DataBindings.Add(new System.Windows.Forms.Binding("Value", bindingsource, col.fieldname, true));

                                dt.Width = maxDatasize;
                                dt.Height = l.Height;
                                dt.ValueChanged += Dt_ValueChanged;
                                dt.Anchor = AnchorStyles.Top;
                                control.Controls.Add(dt);
                                controls.Add(col.fieldname, dt);
                                break;
                            case "System.TimeSpan":
                                t1 = new TextBox
                                {
                                    Left = l.Left + l.Width + 10,
                                    Top = starth
                                };

                                t1.DataBindings.Add(new System.Windows.Forms.Binding("Text", bindingsource, col.fieldname, true));
                                t1.TextAlign = HorizontalAlignment.Left;
                                t1.Width = maxDatasize;
                                t1.Height = l.Height;
                                control.Controls.Add(t1);
                                controls.Add(col.fieldname, t1);
                                t1.TextChanged += T_TextChanged;
                                t1.KeyPress += T_KeyPress;
                                t1.Anchor = AnchorStyles.Top;
                                break;
                            case "System.Boolean":
                                CheckBox ch1 = new CheckBox
                                {
                                    Left = l.Left + l.Width + 10,
                                    Top = starth
                                };

                                ch1.DataBindings.Add(new System.Windows.Forms.Binding("CheckState", bindingsource, col.fieldname, true));
                                ch1.Text = "";
                                ch1.Width = maxDatasize;
                                ch1.Height = l.Height;
                                ch1.CheckStateChanged += Ch1_CheckStateChanged; ;
                                ch1.Anchor = AnchorStyles.Top;
                                control.Controls.Add(ch1);
                                controls.Add(col.fieldname, ch1);

                                break;
                            case "System.Char":
                                MyCheckBox ch2 = new MyCheckBox
                                {
                                    Left = l.Left + l.Width + 10,
                                    Top = starth
                                };

                                ch2.DataBindings.Add(new System.Windows.Forms.Binding("Checked", bindingsource, col.fieldname, true));
                                ch2.Text = "";
                                ch2.Width = maxDatasize;
                                ch2.Height = l.Height;
                                string[] v = coldefaults.propoertValue.Split(',');

                                if (coldefaults != null)
                                {
                                    ch2.TrueValue = v[0].ToCharArray()[0];
                                    ch2.FalseValue = v[1].ToCharArray()[0];
                                }
                                ch2.CheckStateChanged += Ch1_CheckStateChanged; ;
                                ch2.Anchor = AnchorStyles.Top;
                                control.Controls.Add(ch2);
                                controls.Add(col.fieldname, ch2);
                                break;
                            case "System.Int16":
                            case "System.Int32":
                            case "System.Int64":
                            case "System.Decimal":
                            case "System.Double":
                            case "System.Single":
                            case "System.String":
                                if (TableCurrentEntity.Fields.Where(p => p.fieldname == col.fieldname).FirstOrDefault().Size1 > 1)
                                {
                                    t1 = new TextBox
                                    {
                                        Left = l.Left + l.Width + 10,
                                        Top = starth
                                    };

                                    t1.DataBindings.Add(new System.Windows.Forms.Binding("Text", bindingsource, col.fieldname, true));
                                    t1.TextAlign = HorizontalAlignment.Left;
                                    t1.Width = maxDatasize;
                                    t1.Height = l.Height;
                                    
                                    t1.TextChanged += T_TextChanged;
                                    t1.KeyPress += T_KeyPress;
                                    if (TableCurrentEntity.PrimaryKeys.Any(x => x.fieldname == col.fieldname))
                                    {
                                        if (TableCurrentEntity.Relations.Any(x => x.EntityColumnID == col.fieldname))
                                        {
                                            t1.Enabled = false;
                                        }

                                    }
                                    control.Controls.Add(t1);
                                    controls.Add(col.fieldname, t1);
                                    t1.Anchor = AnchorStyles.Top;
                                }
                                else
                                {
                                    ch2 = new MyCheckBox
                                    {
                                        Left = l.Left + l.Width + 10,
                                        Top = starth
                                    };

                                    ch2.DataBindings.Add(new System.Windows.Forms.Binding("Checked", bindingsource, col.fieldname, true));
                                    ch2.Text = "";
                                    ch2.Width = maxDatasize;
                                    ch2.Height = l.Height;



                                    if (coldefaults != null)
                                    {
                                        v = coldefaults.propoertValue.Split(',');
                                        ch2.TrueValue = v[0].ToCharArray()[0];
                                        ch2.FalseValue = v[1].ToCharArray()[0];
                                    }
                                    ch2.CheckStateChanged += Ch1_CheckStateChanged; ;

                                    control.Controls.Add(ch2);
                                    controls.Add(col.fieldname, ch2);
                                    ch2.Anchor = AnchorStyles.Top;
                                }
                                break;
                            default:
                                TextBox t = new TextBox();
                                
                                    t.Left = l.Left + l.Width + 10;
                                    t.Top = starth;
                                    t.DataBindings.Add(new System.Windows.Forms.Binding("Text", bindingsource, col.fieldname, true));
                                    t.TextAlign = HorizontalAlignment.Left;
                                    t.Width = maxDatasize;
                                    t.Height = l.Height;
                                   
                                    t.TextChanged += T_TextChanged;
                                    t.KeyPress += T_KeyPress;
                                    if (TableCurrentEntity.PrimaryKeys.Where(x => x.fieldname == col.fieldname).FirstOrDefault() != null)
                                    {
                                        t.Enabled = false;
                                    }
                                    t.Anchor = AnchorStyles.Top;

                                control.Controls.Add(t);
                                controls.Add(col.fieldname, t);
                                break;

                        }
                    }
                    l.Anchor = AnchorStyles.Top;
                    control.Controls.Add(l);

                    starth = l.Bottom + 1;
                    //this.databaseTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.dataConnectionsBindingSource, "Database", true));

                }

            }
            catch (Exception ex)
            {
                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
                Logger.WriteLog($"Error in Loading View ({ex.Message}) ");
            }
            return Erinfo;
        }
        public IErrorsInfo GenerateFilterFieldsForEntityOnControl(string tbname, ref Panel control, DataTable table, ref BindingSource bindingsource, int width, EntityStructure datahset)
        {

            Erinfo.Flag = Errors.Ok;
            TextBox t1 = new TextBox();

            try
            {
                var starth = 0;
                ViewCurrentEntity = datahset;

                ViewCurrentEntity.Filters = new List<Beep.Report.ReportFilter>();
                foreach (DataColumn col in table.Columns)
                {
                    string coltype = col.DataType.Name.ToString();
                    RelationShipKeys config = ViewCurrentEntity.Relations.Where(f => f.EntityColumnID == col.ColumnName).FirstOrDefault();
                    //----------------------
                    Label l = new Label
                    {
                        Top = starth,
                        Left = 10,
                        AutoSize = false,
                        BorderStyle = BorderStyle.FixedSingle,
                        Text = col.ColumnName
                    };
                    l.Size = TextRenderer.MeasureText(col.ColumnName, l.Font);
                    l.Height += 10;
                    l.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                    l.Width = width;
                    //---------------------

                    if (config != null)
                    {

                        ComboBox cb = new ComboBox
                        {
                            Left = l.Left + l.Width + 10,
                            Top = starth
                        };
                        cb.DataSource = bindingsource;
                        cb.DisplayMember = col.ColumnName;
                        cb.ValueMember = col.ColumnName;
                        cb.Width = width;
                        cb.Height = l.Height;
                        cb.SelectedValueChanged += Cb_SelectedValueChanged;
                        //foreach (DataviewConfig dfc in aFK.Dataviewconfigs)
                        //{
                        //    //cb.Items.Add(dfc.)
                        //}
                        control.Controls.Add(cb);


                    }
                    else
                    {
                        switch (coltype)
                        {
                            case "DateTime":
                                DateTimePicker dt = new DateTimePicker
                                {
                                    Left = l.Left + l.Width + 10,
                                    Top = starth
                                };
                                dt.DataBindings.Add(new System.Windows.Forms.Binding("Value", bindingsource, col.ColumnName, true));

                                dt.Width = width;
                                dt.Height = l.Height;
                                dt.ValueChanged += Dt_ValueChanged;
                                control.Controls.Add(dt);
                                break;
                            case "TimeSpan":
                                t1 = new TextBox
                                {
                                    Left = l.Left + l.Width + 10,
                                    Top = starth
                                };

                                t1.DataBindings.Add(new System.Windows.Forms.Binding("Text", bindingsource, col.ColumnName, true));
                                t1.TextAlign = HorizontalAlignment.Left;
                                t1.Width = width;
                                t1.Height = l.Height;
                                control.Controls.Add(t1);
                                t1.TextChanged += T_TextChanged;
                                t1.KeyPress += T_KeyPress;
                                break;
                            case "Boolean":
                                CheckBox ch1 = new CheckBox
                                {
                                    Left = l.Left + l.Width + 10,
                                    Top = starth
                                };

                                ch1.DataBindings.Add(new System.Windows.Forms.Binding("CheckState", bindingsource, col.ColumnName, true));
                                ch1.Text = "";
                                ch1.Width = width;
                                ch1.Height = l.Height;
                                ch1.CheckStateChanged += Ch1_CheckStateChanged; ;

                                control.Controls.Add(ch1);

                                break;
                            case "Char":
                                break;
                            case "Int16":
                            case "Int32":
                            case "Int64":
                            case "Decimal":
                            case "Double":
                            case "Single":
                            case "String":
                                t1 = new TextBox
                                {
                                    Left = l.Left + l.Width + 10,
                                    Top = starth
                                };

                                t1.DataBindings.Add(new System.Windows.Forms.Binding("Text", bindingsource, col.ColumnName, true));
                                t1.TextAlign = HorizontalAlignment.Left;
                                t1.Width = width;
                                t1.Height = l.Height;
                                control.Controls.Add(t1);
                                t1.TextChanged += T_TextChanged;
                                t1.KeyPress += T_KeyPress;
                                //if (CurrentEntity.PrimeryKeys.Where(x => x.ColumnName == col.ColumnName).FirstOrDefault() != null)
                                //{
                                //    t1.Enabled = false;
                                //}
                                break;



                            default:
                                using (TextBox t = new TextBox())
                                {
                                    t.Left = l.Left + l.Width + 10;
                                    t.Top = starth;
                                    t.DataBindings.Add(new System.Windows.Forms.Binding("Text", bindingsource, col.ColumnName, true));
                                    t.TextAlign = HorizontalAlignment.Left;
                                    t.Width = width;
                                    t.Height = l.Height;
                                    control.Controls.Add(t);
                                    t.TextChanged += T_TextChanged;
                                    t.KeyPress += T_KeyPress;
                                    //if (CurrentEntity.PrimeryKeys.Where(x => x.ColumnName == col.ColumnName).FirstOrDefault() != null)
                                    //{
                                    //    t.Enabled = false;
                                    //}
                                }

                                break;

                        }
                    }

                    control.Controls.Add(l);

                    starth = l.Bottom + 1;
                    //this.databaseTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.dataConnectionsBindingSource, "Database", true));

                }

            }
            catch (Exception ex)
            {

                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
                Logger.WriteLog($"Error in Loading View ({ex.Message}) ");

            }


            return Erinfo;
        }
        public IErrorsInfo GenerateConfigurationViewOnControl(ref Panel control, DataTable table, ref BindingSource bindingsource, int width, EntityStructure datahset)
        {

            Erinfo.Flag = Errors.Ok;
            try
            {
                var starth = 0;
                foreach (DataColumn col in table.Columns)
                {
                    string coltype = col.DataType.Name.ToString();
                    //----------------------
                    Label l = new Label
                    {
                        Top = starth,
                        Left = 10,
                        AutoSize = false,
                        BorderStyle = BorderStyle.FixedSingle,
                        Text = col.ColumnName
                    };
                    l.Size = TextRenderer.MeasureText(col.ColumnName, l.Font);
                    l.Height += 10;
                    l.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                    l.Width = width;
                    //---------------------
                    ComboBox c = new ComboBox
                    {
                        Left = l.Left + l.Width + 10,
                        Top = starth
                    };
                    foreach (var item in Enum.GetValues(typeof(ColumnViewType)))
                    {
                        c.Items.Add(item);
                    }
                    c.DataBindings.Add(new System.Windows.Forms.Binding("Text", bindingsource, col.ColumnName, true));

                    c.Width = width;
                    c.Height = l.Height;
                    control.Controls.Add(c);
                    switch (coltype)
                    {
                        case "DateTime":
                        case "TimeSpan":
                            DateTimePicker dt = new DateTimePicker
                            {
                                Left = l.Left + l.Width + 10,
                                Top = starth
                            };
                            dt.DataBindings.Add(new System.Windows.Forms.Binding("Value", bindingsource, col.ColumnName, true));

                            dt.Width = width;
                            dt.Height = l.Height;
                            control.Controls.Add(dt);
                            break;
                        case "Boolean":
                            break;
                        case "Char":
                            break;
                        case "Int16":
                        case "Int32":
                        case "Int64":
                        case "Decimal":
                        case "Double":
                        case "Single":
                            break;
                        case "String":

                        default:
                            TextBox t = new TextBox
                            {
                                Left = l.Left + l.Width + 10,
                                Top = starth
                            };
                            t.DataBindings.Add(new System.Windows.Forms.Binding("Text", bindingsource, col.ColumnName, true));
                            t.TextAlign = HorizontalAlignment.Left;
                            t.Width = width;
                            t.Height = l.Height;
                            control.Controls.Add(t);
                            break;

                    }
                    control.Controls.Add(l);

                    starth = l.Bottom + 1;
                    //this.databaseTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.dataConnectionsBindingSource, "Database", true));

                }

            }
            catch (Exception ex)
            {

                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
                Logger.WriteLog($"Error in Loading  Control {ex.Message}) ");

            }


            return Erinfo;
        }
        public DialogResult InputBoxYesNo(string title, string promptText)
        {
            Form form = new Form();
            Label label = new Label();
         //   TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
          //  textBox.Text = value;

            buttonOk.Text = "Yes";
            buttonCancel.Text = "No";
            buttonOk.DialogResult = (System.Windows.Forms.DialogResult)DialogResult.Yes;
            buttonCancel.DialogResult = (System.Windows.Forms.DialogResult)DialogResult.No;

            label.SetBounds(9, 20, 372, 13);
           // textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            //textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = (DialogResult)form.ShowDialog();
           // value = textBox.Text;
            return dialogResult;
        }
        public DialogResult InputBox(string title, string promptText,ref string  value)
        {
            Form form = new Form();
            Label label = new Label();
             TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
             textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = (System.Windows.Forms.DialogResult)DialogResult.OK;
            buttonCancel.DialogResult = (System.Windows.Forms.DialogResult)DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
           textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, buttonOk,textBox, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = (DialogResult)form.ShowDialog();
             value = textBox.Text;
            return dialogResult;
        }
        public void MsgBox(string title, string promptText)
        {

            try
            {
                MessageBox.Show(promptText,title);

                DMEEditor.AddLogMessage("Success", "Created Msgbox", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not create msgbox";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            
        }
        public DialogResult InputComboBox(string title, string promptText, List<string> itvalues, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            // TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();
            ComboBox combo = new ComboBox { Left = 50, Top = 50, Width = 400 };

            form.Text = title;
            label.Text = promptText;
            combo.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = (System.Windows.Forms.DialogResult)DialogResult.OK;
            buttonCancel.DialogResult = (System.Windows.Forms.DialogResult)DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            combo.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            combo.Anchor = combo.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            foreach (string c in itvalues)
            {
                var t = combo.Items.Add(c);

            }
            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, combo, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = (DialogResult)form.ShowDialog();
            value = combo.Text;
            return dialogResult;
        }
        public static string DialogCombo(string text, DataTable comboSource, string DisplyMember, string ValueMember)
        {
            //comboSource = new DataTable();


            Form prompt = new Form();
            //prompt.RightToLeft = RightToLeft.Yes;
            prompt.Width = 500;
            prompt.Height = 200;
            Label textLabel = new Label() { Left = 350, Top = 20, Text = text };
            ComboBox combo = new ComboBox { Left = 50, Top = 50, Width = 400 };
            combo.DataSource = comboSource;
            combo.ValueMember = ValueMember;
            combo.DisplayMember = DisplyMember;
            Button confirmation = new Button() { Text = "Submit", Left = 350, Width = 100, Top = 70 };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(combo);
            prompt.ShowDialog();

            return combo.SelectedValue.ToString();
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
        public void CreateEntityFilterControls(Control panel, EntityStructure entityStructure, List<DefaultValue> dsdefaults)
        {
           
            BindingSource[]  BindingData = new BindingSource[entityStructure.Fields.Count ];
            int maxlabelsize = 0;
            int maxDatasize = 0;
            foreach (EntityField col in entityStructure.Fields)
            {
                int x = getTextSize(col.fieldname.ToUpper());
                if (maxlabelsize < x)
                    maxlabelsize = x;
            }
            maxDatasize = panel.Width - maxlabelsize - 20;
            entityStructure.Filters.Clear();
            List<string> FieldNames = new List<string>();
            var starth = 25;
            int startleft = maxlabelsize + 90;
            int valuewidth = 100;
            for (int i = 0; i <= entityStructure.Fields.Count-1 ; i++)
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
                        Text = col.fieldname.ToUpper(),
                        BackColor = Color.White,
                        ForeColor = Color.Red

                    };
                    l.Size = TextRenderer.MeasureText(col.fieldname.ToUpper(), l.Font);
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
                    cbcondition.Name = col.fieldname + "." + "Operator";
                    cbcondition.Width = 50;
                    cbcondition.Height = l.Height;
                    // cbcondition.SelectedText
                    cbcondition.DataBindings.Add(new System.Windows.Forms.Binding("SelectedValue", BindingData[i], "Operator", true, DataSourceUpdateMode.OnPropertyChanged));
                    //  cbcondition.Anchor = AnchorStyles.Top;
                    panel.Controls.Add(cbcondition);

                    if (FK != null)
                    {
                        ComboBox cb = new ComboBox
                        {
                            Left = startleft,
                            Top = starth
                        };
                        string DisplayField = FK.EntityColumnID;
                        cb.DataSource = GetDisplayLookup(entityStructure.DataSourceID, FK.ParentEntityID, FK.ParentEntityColumnID);
                        cb.DisplayMember = DisplayField;
                        cb.ValueMember = FK.ParentEntityColumnID;
                        cb.Width = valuewidth;
                        cb.Height = l.Height;
                        cb.DataBindings.Add(new System.Windows.Forms.Binding("TEXT", BindingData[i], "FilterValue", true));
                        cb.SelectedValueChanged += Cb_SelectedValueChanged;
                        //cb.Anchor = AnchorStyles.Top;
                        panel.Controls.Add(cb);
                       cb.Name = col.fieldname ;
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
                                dt.Name = "From" + "." + col.fieldname;
                                panel.Controls.Add(dt);
                                DateTimePicker dt1 = new DateTimePicker
                                {
                                    Left = dt.Left + 10 + dt.Width,
                                    Top = starth
                                };
                                dt1.DataBindings.Add(new System.Windows.Forms.Binding("Value", BindingData[i], "FilterValue1", true));

                                dt1.Width = valuewidth;
                                dt1.Height = l.Height;
                                dt1.Format = DateTimePickerFormat.Short;
                                dt1.ValueChanged += Dt_ValueChanged;
                                //     dt.Anchor = AnchorStyles.Top;
                                dt1.Tag = i;
                                dt1.Name = "From" + "."+col.fieldname;
                                panel.Controls.Add(dt1);
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
                                t1.Name = col.fieldname;
                                panel.Controls.Add(t1);
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
                                ch1.Name = col.fieldname;
                                panel.Controls.Add(ch1);


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
                                ch2.Name =  col.fieldname;
                                panel.Controls.Add(ch2);

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
                                panel.Controls.Add(t3);

                                NumericUpDown t2 = new NumericUpDown();
                                t2.Left = t3.Left + t3.Width + 10;
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

                                panel.Controls.Add(t2);
                                //   t.Anchor = AnchorStyles.Top;
                                break;

                            case "System.String":
                                if (entityStructure.Fields.Where(p => p.fieldname == col.fieldname).FirstOrDefault().Size1 != 1)
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
                                    panel.Controls.Add(t1);
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

                                    panel.Controls.Add(ch2);

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

                                panel.Controls.Add(t);

                                break;

                        }
                    }
                    // l.Anchor = AnchorStyles.Top;
                    panel.Controls.Add(l);
                    starth = l.Bottom + 1;

                }
                catch (Exception ex)
                {

                    Logger.WriteLog($"Error in Loading View ({ex.Message}) ");
                }

            }


        }
        #endregion "Create Controls Dynamiclly"
        #region Handling Controls and binding
        //dataGridViewTextBoxColumn
        public virtual IErrorsInfo BindDataSourceEnumtoComobo(DataGridViewComboBoxColumn combo)
        {
            Erinfo.Flag = Errors.Ok;


            try
            {

                combo.DataSource = Enum.GetValues(typeof(DatasourceCategory)).Cast<DatasourceCategory>()
                                              .Select(x => new DatasourceCategoryDataItem() { Value = x, Text = x.ToString() })
                                              .ToList();
                combo.ValueMember = "Value";
                combo.DisplayMember = "Text";

            }
            catch (System.Exception ex)
            {

                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
                Logger.WriteLog($"Error in Binding DataSourceEnum to Comobo ({ex.Message})");
            }
            return Erinfo;



        }
        public virtual IErrorsInfo BindDataSourceEnumtoComobo(ComboBox combo)
        {
            Erinfo.Flag = Errors.Ok;


            try
            {

                combo.DataSource = Enum.GetValues(typeof(DatasourceCategory)).Cast<DatasourceCategory>()
                                              .Select(x => new DatasourceCategoryDataItem() { Value = x, Text = x.ToString() })
                                              .ToList();
                combo.ValueMember = "Value";
                combo.DisplayMember = "Text";

            }
            catch (System.Exception ex)
            {

                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
                Logger.WriteLog($"Error in Binding DataSourceEnum to Comobo ({ex.Message})");
            }
            return Erinfo;



        }

        #endregion
    }


}
          