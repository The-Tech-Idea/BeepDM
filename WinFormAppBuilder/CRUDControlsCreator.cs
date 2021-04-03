using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.AppBuilder
{
    public class CRUDControlsCreator
    {
        public event EventHandler<PassedArgs> ActionNeeded;
        public CRUDControlsCreator()
        {

        }
        public DMEEditor DMEEditor { get; set; }
     
        public EntityStructure CurrentEntity { get; set; }
        public void RaiseEvent(object sender, EventArgs e, Type SenderType)
        {
            dynamic tx = null;
            switch (SenderType.Name)
            {
                case "TextBox":
                    break;
                    tx = (TextBox)sender;
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
        #region "Detect Changes with Dynamiclly generated userControl"
        private void SetFilterPropertyValue(string Fieldname, string Value)
        {
            ReportFilter ent = CurrentEntity.Filters.Where(x => x.FieldName == Fieldname).FirstOrDefault();
            if (ent == null)
            {
                ent = new ReportFilter();
                ent.FieldName = Fieldname;
                ent.FilterValue = Value;
                CurrentEntity.Filters.Add(ent);
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
                tx.Text = "";
            }
            else
            {
                SetFilterPropertyValue(tx.Name, tx.Text);
            }
            RaiseEvent(sender, e, typeof(TextBox));


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
        private DataTable GetDisplayLookup(string datasourceid, string entityname, string KeyField, ref string DisplayField)
        {
            DataTable retval = new DataTable(entityname);
            try
            {
                IDataSource ds = DMEEditor.GetDataSource(datasourceid);
                EntityStructure ent = ds.GetEntityStructure(entityname, false);
                bool found = true;
                int i = 0;

                do
                {
                    if (ent.Fields[i].fieldname != KeyField && ent.Fields[i].fieldtype == "System.String")
                    {
                        DisplayField = CurrentEntity.Fields[i].fieldname;
                        found = false;

                    }
                    i += 1;
                } while (found);
                string qrystr = "select " + DisplayField + "," + KeyField + " from " + entityname;



                retval = ds.RunQuery(qrystr);

                return retval;
            }
            catch (Exception )
            {

                return null;
            }
        }
        #endregion "Detect Changes with Dynamiclly generated userControl"
        public Control CreateCRUDControl(string entityname, IDataSource ds, DataTable table, ref BindingSource bindingsource, int width, string datasourceid)
        {
            Control control = new Control();
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            TextBox t1 = new TextBox();
            
            CurrentEntity = ds.GetEntityStructure(entityname, false);
            // Create Filter Control
            // CreateFilterQueryGrid(entityname, CurrentEntity.Fields, null);
            try
            {
                var starth = 0;


                CurrentEntity.Filters = new List<DataManagment_Engine.Report.ReportFilter>();
                foreach (DataColumn col in table.Columns)
                {
                    string coltype = col.DataType.Name.ToString();
                    RelationShipKeys FK = CurrentEntity.Relations.Where(f => f.EntityColumnID == col.ColumnName).FirstOrDefault();
                    //----------------------
                    Label l = new Label
                    {
                        Top = starth,
                        Left = 10,
                        AutoSize = false,
                        BorderStyle = BorderStyle.FixedSingle,
                        Text = col.ColumnName,
                        BackColor = Color.White,
                        ForeColor = Color.Red

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
                        string DisplayField = "";
                        cb.DataSource = GetDisplayLookup(datasourceid, FK.ParentEntityID, FK.ParentEntityColumnID, ref DisplayField);
                        cb.DisplayMember = DisplayField;
                        cb.ValueMember = FK.ParentEntityColumnID;
                        cb.Width = width;
                        cb.Height = l.Height;
                        cb.DataBindings.Add(new System.Windows.Forms.Binding("TEXT", bindingsource, col.ColumnName, true));
                        cb.SelectedValueChanged += Cb_SelectedValueChanged;
                      

                        control.Controls.Add(cb);
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
                                if (CurrentEntity.PrimaryKeys.Where(x => x.fieldname == col.ColumnName).FirstOrDefault() != null)
                                {
                                    t1.Enabled = false;
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
                                    t.TextChanged += T_TextChanged;
                                    t.KeyPress += T_KeyPress;
                                    if (CurrentEntity.PrimaryKeys.Where(x => x.fieldname == col.ColumnName).FirstOrDefault() != null)
                                    {
                                        t.Enabled = false;
                                    }
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

                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
                DMEEditor.Logger.WriteLog($"Error in Loading View ({ex.Message}) ");

            }
            return control;
        }
    }
}
