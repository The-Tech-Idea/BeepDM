using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.AppManager
{
    public class AppTemplate : IAppDefinition
    {
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public List<AppBlock> Blocks { get; set; } = new List<AppBlock>();
        public string DataSourceName { get; set; }
        public TextBlock Description { get; set; } = new TextBlock();
     
        public string Name { get; set; }
        public string ReportEndText { get; set; }
        public TextBlock SubTitle { get; set; } = new TextBlock();
        public AppOrientation Orientation { get; set; }
        public TextBlock Title { get; set; } = new TextBlock();
        public TextBlock Header { get; set; } = new TextBlock();
        public TextBlock Footer { get; set; } = new TextBlock();
        public string CSS { get; set; }
        public int ViewID { get; set; }
        public AppTemplate()
        {
            Orientation = AppOrientation.Portrait;
        }
        public AppTemplate(string pHeader, string pFooter, string pTitle, string pSubTitle)
        {
            Header.Text = pHeader;
            Footer.Text = pFooter;
            Title.Text = pTitle;
            SubTitle.Text = pSubTitle;
            Orientation = AppOrientation.Portrait;
            GuidID = Guid.NewGuid().ToString();
        }
        public AppTemplate(string pHeader, string pFooter, string pTitle, string pSubTitle, AppOrientation pOrientation, string pDataSourceName)
        {
            Header.Text = pHeader;
            Footer.Text = pFooter;
            Title.Text = pTitle;
            SubTitle.Text = pSubTitle;
            Orientation = pOrientation;
            DataSourceName = pDataSourceName;
            GuidID = Guid.NewGuid().ToString();
        }
        public AppTemplate(string pHeader, string pFooter, string pTitle, string pSubTitle, AppOrientation pOrientation, string pDataSourceName, List<AppBlock> pBlocks)
        {
            Header.Text = pHeader;
            Footer.Text = pFooter;
            Title.Text = pTitle;
            SubTitle.Text = pSubTitle;
            Orientation = pOrientation;
            DataSourceName = pDataSourceName;
            Blocks = pBlocks;
            GuidID = Guid.NewGuid().ToString();
        }
    }
}
