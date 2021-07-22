using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.Report
{
    public class ReportTemplate : IReportDefinition
    {
        public string ID { get; set; }
        public List<ReportBlock> Blocks { get; set; } = new List<ReportBlock>();
        public string DataSourceName { get; set; }
        public TextBlock Description { get; set; } = new TextBlock();
        public List<ReportFilter> filters { get; set; } = new List<ReportFilter>();
        public string Name { get; set; }
        public string ReportEndText { get; set; }
        public TextBlock SubTitle { get; set; } = new TextBlock();
        public ReportOrientation Orientation { get; set; }
        public TextBlock Title { get; set; } = new TextBlock();
        public TextBlock Header { get; set; } = new TextBlock();
        public TextBlock Footer { get; set; } = new TextBlock();
        public string CSS { get; set; }
        public int ViewID { get; set; }
        public ReportTemplate()
        {
            Orientation = ReportOrientation.Portrait;
        }
        public ReportTemplate(string pHeader, string pFooter, string pTitle, string pSubTitle)
        {
            Header.Text = pHeader;
            Footer.Text = pFooter;
            Title.Text = pTitle;
            SubTitle.Text = pSubTitle;
            Orientation = ReportOrientation.Portrait;
            ID = Guid.NewGuid().ToString();
        }
        public ReportTemplate(string pHeader, string pFooter, string pTitle, string pSubTitle, ReportOrientation pOrientation, string pDataSourceName)
        {
            Header.Text = pHeader;
            Footer.Text = pFooter;
            Title.Text = pTitle;
            SubTitle.Text = pSubTitle;
            Orientation = pOrientation;
            DataSourceName = pDataSourceName;
            ID = Guid.NewGuid().ToString();
        }
        public ReportTemplate(string pHeader, string pFooter, string pTitle, string pSubTitle, ReportOrientation pOrientation, string pDataSourceName, List<ReportBlock> pBlocks)
        {
            Header.Text = pHeader;
            Footer.Text = pFooter;
            Title.Text = pTitle;
            SubTitle.Text = pSubTitle;
            Orientation = pOrientation;
            DataSourceName = pDataSourceName;
            Blocks = pBlocks;
            ID = Guid.NewGuid().ToString();
        }
    }
}
