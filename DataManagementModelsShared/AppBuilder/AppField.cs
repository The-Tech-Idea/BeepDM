using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using TheTechIdea.DataManagment_Engine.DataBase;

namespace TheTechIdea.DataManagment_Engine.AppBuilder
{
   

    public class AppField : IAppField
    {
        public AppField()
        {
                
        }



        public string datasourcename { get; set; }
        public string entityname { get; set; }
        public string Appname { get; set; }
     
        public string fieldname { get; set; }
        public string label { get; set; }
        public DisplayFieldTypes fieldTypes { get; set; } = DisplayFieldTypes.Textbox;
        public int x { get; set; }
        public int y { get; set; }
        public int offsetx { get; set; }
        public int offsety { get; set; }
        public bool displaylabel { get; set; } = true;
        public bool checkboxOtherValues { get; set; } = false;
        public string checkboxTrueValue { get; set; } = "Y";
        public string checkboxFalseValue { get; set; } = "N";
        public Font fieldFont { get; set; } = new Font("Times New Roman", 12.0f);
        public Color fieldForeColor { get; set; } = Color.Black;
        public Color fieldBackColor { get; set; } = Color.Transparent;
        public Color fieldAlternatingBackColor { get; set; } = Color.Transparent;
        public Color fieldBorderLineColor { get; set; } = Color.Transparent;
        public Font labelFont { get; set; } = new Font("Times New Roman", 12.0f);
        public Color labelForeColor { get; set; } = Color.Black;
        public Color labelBackColor { get; set; } = Color.Transparent;
        public Color labelAlternatingBackColor { get; set; } = Color.Transparent;
        public Color labelBorderLineColor { get; set; } = Color.Transparent;
        public string lookupValue { get; set; }
        public string lookupDisplay { get; set; }
        public string lookupEntity { get; set; }
        public bool enabled { get; set; }
        public bool readOnly { get; set; }
        public bool autofill { get; set; }
        public bool ValueRetrievedFromParent { get; set; }


    }

    public enum DisplayFieldTypes
{
    Textbox, ComboBox, List, CheckBox, Calendar, Label
}

}
