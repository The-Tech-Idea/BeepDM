using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.AppManager
{
    public interface IAppBlockColumns
    {
         string ID { get; set; }
         string GuidID { get; set; } 
        string ColumnName { get; set; }
        int ColumnSeq { get; set; }
        string DisplayName { get; set; }
        int FieldDisplaySeq { get; set; }
        bool Show { get; set; }
         int LocationY { get; set; }
         int LocationX { get; set; }
        
         Color ForeColor { get; set; }
         Color BackColor { get; set; }
         Color AlternatingBackColor { get; set; }
         Color BorderLineColor { get; set; }
    }

    public class AppBlockColumns : Entity, IAppBlockColumns
    {
        public AppBlockColumns()
        {

        }
        private string _id;
        public string ID
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _guidid = Guid.NewGuid().ToString();
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        } 

        private string _columnname;
        public string ColumnName
        {
            get { return _columnname; }
            set { SetProperty(ref _columnname, value); }
        }

        private string _displayname;
        public string DisplayName
        {
            get { return _displayname; }
            set { SetProperty(ref _displayname, value); }
        }

        private int _columnseq;
        public int ColumnSeq
        {
            get { return _columnseq; }
            set { SetProperty(ref _columnseq, value); }
        }

        private int _fielddisplayseq;
        public int FieldDisplaySeq
        {
            get { return _fielddisplayseq; }
            set { SetProperty(ref _fielddisplayseq, value); }
        }

        private bool _show = true;
        public bool Show
        {
            get { return _show; }
            set { SetProperty(ref _show, value); }
        } 

        private int _locationy = 0;
        public int LocationY
        {
            get { return _locationy; }
            set { SetProperty(ref _locationy, value); }
        }

        private int _locationx = 0;
        public int LocationX
        {
            get { return _locationx; }
            set { SetProperty(ref _locationx, value); }
        } 


        private Color _forecolor = Color.Black;
        public Color ForeColor
        {
            get { return _forecolor; }
            set { SetProperty(ref _forecolor, value); }
        } 

        private Color _backcolor = Color.Transparent;
        public Color BackColor
        {
            get { return _backcolor; }
            set { SetProperty(ref _backcolor, value); }
        } 

        private Color _alternatingbackcolor = Color.Transparent;
        public Color AlternatingBackColor
        {
            get { return _alternatingbackcolor; }
            set { SetProperty(ref _alternatingbackcolor, value); }
        }

        private Color _borderlinecolor = Color.Transparent;
        public Color BorderLineColor
        {
            get { return _borderlinecolor; }
            set { SetProperty(ref _borderlinecolor, value); }
        } 
    }
}
