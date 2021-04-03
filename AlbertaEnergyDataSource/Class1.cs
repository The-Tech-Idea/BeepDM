using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.DataManagment_Engine.WebAPI.AlbertaEnergy
{
    public class ConfidentialWell
    {
        public  string WellLocation { get; set; }
        public int LicenceNo { get; set; }
        public string LicenseeCodeName { get; set; }
        public string ConfidentialType { get; set; }
        public int ConfBelowFrmtn { get; set; }
        public string ConfReleaseDate { get; set; }

    }
}
