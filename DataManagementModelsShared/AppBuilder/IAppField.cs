using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.AppBuilder
{
    public interface IAppField
    {
        string datasourcename { get; set; }
        string entityname { get; set; }
        string fieldname { get; set; }
        string Appname { get; set; }
        bool autofill { get; set; }
        string checkboxFalseValue { get; set; }
        bool checkboxOtherValues { get; set; }
        string checkboxTrueValue { get; set; }
        bool displaylabel { get; set; }
        bool enabled { get; set; }
       
        Color fieldAlternatingBackColor { get; set; }
        Color fieldBackColor { get; set; }
        Color fieldBorderLineColor { get; set; }
        Font fieldFont { get; set; }
        Color fieldForeColor { get; set; }
        DisplayFieldTypes fieldTypes { get; set; }
        string label { get; set; }
        Color labelAlternatingBackColor { get; set; }
        Color labelBackColor { get; set; }
        Color labelBorderLineColor { get; set; }
        Font labelFont { get; set; }
        Color labelForeColor { get; set; }
        bool ValueRetrievedFromParent { get; set; }
        string lookupDisplay { get; set; }
        string lookupEntity { get; set; }
        string lookupValue { get; set; }
        int offsetx { get; set; }
        int offsety { get; set; }
        bool readOnly { get; set; }
       
        int x { get; set; }
        int y { get; set; }
    }
}
