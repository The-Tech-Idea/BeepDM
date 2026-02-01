using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Editor
{
    public class TrackedEntity :Entity
    {
        private string ACTIVE_INDValue = "Y";
        public string ACTIVE_IND { get => ACTIVE_INDValue; set => SetProperty(ref ACTIVE_INDValue, value); }

        private string ROW_CREATED_BYValue = string.Empty;
        public string ROW_CREATED_BY { get => ROW_CREATED_BYValue; set => SetProperty(ref ROW_CREATED_BYValue, value); }

        private DateTime? ROW_CREATED_DATEValue;
        public DateTime? ROW_CREATED_DATE { get => ROW_CREATED_DATEValue; set => SetProperty(ref ROW_CREATED_DATEValue, value); }

        private string ROW_CHANGED_BYValue = string.Empty;
        public string ROW_CHANGED_BY { get => ROW_CHANGED_BYValue; set => SetProperty(ref ROW_CHANGED_BYValue, value); }

        private DateTime? ROW_CHANGED_DATEValue;
        public DateTime? ROW_CHANGED_DATE { get => ROW_CHANGED_DATEValue; set => SetProperty(ref ROW_CHANGED_DATEValue, value); }

        private DateTime? ROW_EFFECTIVE_DATEValue;
        public DateTime? ROW_EFFECTIVE_DATE { get => ROW_EFFECTIVE_DATEValue; set => SetProperty(ref ROW_EFFECTIVE_DATEValue, value); }

        private DateTime? ROW_EXPIRY_DATEValue;
        public DateTime? ROW_EXPIRY_DATE { get => ROW_EXPIRY_DATEValue; set => SetProperty(ref ROW_EXPIRY_DATEValue, value); }

        private string ROW_QUALITYValue = string.Empty;
        public string ROW_QUALITY { get => ROW_QUALITYValue; set => SetProperty(ref ROW_QUALITYValue, value); }

        private string Entity_GUIDValue = string.Empty;
        public string ENTITY_GUID { get => Entity_GUIDValue; set => SetProperty(ref Entity_GUIDValue, value); }
        private System.DateTime? EXPIRY_DATEValue;
        public System.DateTime? EXPIRY_DATE
        {
            get
            {
                return this.EXPIRY_DATEValue;
            }

            set { SetProperty(ref EXPIRY_DATEValue, value); }
        }
        private System.DateTime? EFFECTIVE_DATEValue;
        public System.DateTime? EFFECTIVE_DATE
        {
            get
            {
                return this.EFFECTIVE_DATEValue;
            }

            set { SetProperty(ref EFFECTIVE_DATEValue, value); }
        }
        private System.String REMARKValue;
        public System.String REMARK
        {
            get
            {
                return this.REMARKValue;
            }

            set { SetProperty(ref REMARKValue, value); }
        }
        private System.String SOURCEValue;
        public System.String SOURCE
        {
            get
            {
                return this.SOURCEValue;
            }

            set { SetProperty(ref SOURCEValue, value); }
        }
    }
}
