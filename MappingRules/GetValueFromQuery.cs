using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Workflow
{
    public class GetValueFromQuery : IMapping_Rule
    {
        public string RuleName { get; set; } = "GetValueFromQuery";
        public string Rule { get; set; } = "GetValueFromQuery";
        public IDMEEditor DMEEditor { get; set; }
        public PassedArgs ExecuteRule(PassedArgs args)
        {
            try
            {


                IDataSource ds = DMEEditor.GetDataSource(args.DatasourceName);
                object tb = ds.RunQuery(args.ParameterString1);
                dynamic v1=null;
                //if (tb != null)
                //{
                //    if (tb.Rows.Count > 0)
                //    {
                //       v1  = tb.Rows[0].ItemArray[0].ToString();
                //    }
                //}
                ObjectItem it = new ObjectItem();
                it.obj = v1;
                it.Name = "GetDefaultValue";

                if (args.Objects == null)
                {
                    args.Objects = new List<ObjectItem>();
                }
                args.Objects.Add(it);

            }

            catch (Exception ex)
            {
                string mes = "Could not Execute Rule " + RuleName;
                DMEEditor.AddLogMessage("Fail", mes + ex.Message, DateTime.Now, -1, mes, Errors.Failed);
            };
            return args;
        }
        public GetValueFromQuery()
        {

        }
    }
}
