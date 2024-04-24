using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.AppManager
{
    public interface IAppBlock
    {
         int ID { get; set; }
        string GuidID { get; set; } 
        string Title { get; set; }
        List<AppBlockColumns> BlockColumns { get; set; }
        List<AppFilter> filters { get; set; }
        string EntityID { get; set; }
        string ViewID { get; set; }

        int LocationY { get; set; }
        int LocationX { get; set; }
        string CustomBuildQuery { get; set; }
        Color ForeColor { get; set; }
        Color HeaderForeColor { get; set; }
        Color HeaderBackColor { get; set; }

        Color BackColor { get; set; }
        Color AlternatingBackColor { get; set; }
        Color GridLineColor { get; set; }
        DataGridLineStyle GridLineStyle { get; set; }
        BlockViewType ViewType { get; set; }
        List<EntityField> Fields { get; set; }
        List<EntityParameters> Paramenters { get; set; }
        List<RelationShipKeys> Relations { get; set; }
    }

    public class AppBlock : Entity, IAppBlock
 {
     
private int _id;
    public int ID
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

    private string _title;
    public string Title
    {
        get { return _title; }
        set { SetProperty(ref _title, value); }
    }

    private string _entityid;
    public string EntityID
    {
        get { return _entityid; }
        set { SetProperty(ref _entityid, value); }
    }

    private string _viewid;
    public string ViewID
    {
        get { return _viewid; }
        set { SetProperty(ref _viewid, value); }
    }

    private BlockViewType _viewtype;
    public BlockViewType ViewType
    {
        get { return _viewtype; }
        set { SetProperty(ref _viewtype, value); }
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

    private Color _backcolor;
    public Color BackColor
    {
        get { return _backcolor; }
        set { SetProperty(ref _backcolor, value); }
    }

    private Color _alternatingbackcolor;
    public Color AlternatingBackColor
    {
        get { return _alternatingbackcolor; }
        set { SetProperty(ref _alternatingbackcolor, value); }
    }

    private Color _gridlinecolor;
    public Color GridLineColor
    {
        get { return _gridlinecolor; }
        set { SetProperty(ref _gridlinecolor, value); }
    }

    private DataGridLineStyle _gridlinestyle;
    public DataGridLineStyle GridLineStyle
    {
        get { return _gridlinestyle; }
        set { SetProperty(ref _gridlinestyle, value); }
    }

    private string _custombuildquery;
    public string CustomBuildQuery
    {
        get { return _custombuildquery; }
        set { SetProperty(ref _custombuildquery, value); }
    }

    private List<AppFilter> _filters = new List<AppFilter>();
        public List<AppFilter> filters
    {
        get { return _filters; }
        set { SetProperty(ref _filters, value); }
    }

    private List<AppBlockColumns> _blockcolumns = new List<AppBlockColumns>();
        public List<AppBlockColumns> BlockColumns
    {
        get { return _blockcolumns; }
        set { SetProperty(ref _blockcolumns, value); }
    } 

    private List<EntityField> _fields = new List<EntityField>();
        public List<EntityField> Fields
    {
        get { return _fields; }
        set { SetProperty(ref _fields, value); }
    } 

    private List<EntityParameters> _paramenters = new List<EntityParameters>();
        public List<EntityParameters> Paramenters
    {
        get { return _paramenters; }
        set { SetProperty(ref _paramenters, value); }
    } 

    private List<RelationShipKeys> _relations = new List<RelationShipKeys>();
        public List<RelationShipKeys> Relations
    {
        get { return _relations; }
        set { SetProperty(ref _relations, value); }
    } 

    private Color _headerforecolor;
    public Color HeaderForeColor
    {
        get { return _headerforecolor; }
        set { SetProperty(ref _headerforecolor, value); }
    }

    private Color _headerbackcolor;
    public Color HeaderBackColor
    {
        get { return _headerbackcolor; }
        set { SetProperty(ref _headerbackcolor, value); }
    }


    public AppBlock()
    {

    }
}
public enum BlockViewType
    {
        Table, Details, Graph
    }
    public class TextBlock : Entity
    {
        public TextBlock()
        {

        }

        private int _id;
        public int ID
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

        private string _text;
        public string Text
        {
            get { return _text; }
            set { SetProperty(ref _text, value); }
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

        private Color _linecolor = Color.Black;
        public Color LineColor
        {
            get { return _linecolor; }
            set { SetProperty(ref _linecolor, value); }
        } 

    }
    public enum DataGridLineStyle
    {
        //
        // Summary:
        //     No gridlines between cells.
        None = 0,
        //
        // Summary:
        //     Solid gridlines between cells.
        Solid = 1
    }
}
