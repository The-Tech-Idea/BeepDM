using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Vis
{
    public class KeyCombination : Entity
    {
        private BeepKeys _key;
        public BeepKeys Key
        {
            get { return _key; }
            set { SetProperty(ref _key, value); }
        }
        private bool _control;
        public bool Control
        {
            get { return _control; }
            set { SetProperty(ref _control, value); }
        }
        private bool _alt;
        public bool Alt
        {
            get { return _alt; }
            set { SetProperty(ref _alt, value); }
        }
        private bool _shift;
        public bool Shift
        {
            get { return _shift; }
            set { SetProperty(ref _shift, value); }
        }
        private MethodsClass _mappedFunction;
        public MethodsClass MappedFunction
        {
            get { return _mappedFunction; }
            set { SetProperty(ref _mappedFunction, value); }
        }
        private string _className;
        public string ClassName
        {
            get { return _className; }
            set { SetProperty(ref _className, value); }
        }
        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }
        private string _description;
        public string Description
        {
            get { return _description; }
            set { SetProperty(ref _description, value); }
        }
        private bool _handled;
        public bool Handled
        {
            get { return _handled; }
            set { SetProperty(ref _handled, value); }
        }
        private bool _enabled;
        public bool Enabled
        {
            get { return _enabled; }
            set { SetProperty(ref _enabled, value); }
        }
        private bool _applyOnForm;
        public bool ApplyOnForm
        {
            get { return _applyOnForm; }
            set { SetProperty(ref _applyOnForm, value); }
        }
        private bool _applyOnControl;
        public bool ApplyOnControl
        {
            get { return _applyOnControl; }
            set { SetProperty(ref _applyOnControl, value); }
        }
        private bool _applyOnGrid;
        public bool ApplyOnGrid
        {
            get { return _applyOnGrid; }
            set { SetProperty(ref _applyOnGrid, value); }
        }
        private bool _applyOnTree;
        public bool ApplyOnTree
        {
            get { return _applyOnTree; }
            set { SetProperty(ref _applyOnTree, value); }
        }
        private bool _applyOnList;
        public bool ApplyOnList
        {
            get { return _applyOnList; }
            set { SetProperty(ref _applyOnList, value); }
        }
        private bool _applyOnReport;
        public bool ApplyOnReport
        {
            get { return _applyOnReport; }
            set { SetProperty(ref _applyOnReport, value); }
        }
        private bool _applyOnChart;
        public bool ApplyOnChart
        {
            get { return _applyOnChart; }
            set { SetProperty(ref _applyOnChart, value); }
        }
        
        private string _assemblyguid;
        public string AssemblyGuid
        {
            get { return _assemblyguid; }
            set { SetProperty(ref _assemblyguid, value); }
        }

        public KeyCombination()
        {

        }

        public KeyCombination(BeepKeys key, bool control, bool alt, bool shift)
        {
            Key = key;
            Control = control;
            Alt = alt;
            Shift = shift;
        }

        public override bool Equals(object obj)
        {
            return obj is KeyCombination kc &&
                   Key == kc.Key &&
                   Control == kc.Control &&
                   Alt == kc.Alt &&
                   Shift == kc.Shift;
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                // Suitable prime numbers
                hash = hash * 23 + Key.GetHashCode();
                hash = hash * 23 + Control.GetHashCode();
                hash = hash * 23 + Alt.GetHashCode();
                hash = hash * 23 + Shift.GetHashCode();
                return hash;
            }
        }
    }

}
