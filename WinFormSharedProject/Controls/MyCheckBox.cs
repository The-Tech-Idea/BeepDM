using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace WinFormSharedProject.Controls
{
    

    public class MyCheckBox : CheckBox
    {
        public char TrueValue { get; set; } = 'Y';
        public char FalseValue { get; set; } = 'N';
        public new char Checked
        {
            get
            {
                return BooleanToString(base.Checked);
            }
            set
            {
                base.Checked = StringToBoolean(value);
            }
        }

        private char BooleanToString(bool b)
        {
            return (b)? TrueValue : FalseValue;
        }

        private bool StringToBoolean(char s)
        {
            return (s== TrueValue) ? true: false;
        }
    }

}
