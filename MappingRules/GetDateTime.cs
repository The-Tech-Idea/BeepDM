using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Workflow
{
    public class GetDateTime : IMapping_Rule
    {
        public string RuleName { get; set; } = "GetDateTime";
        public string Rule { get; set; } = "GetDateTime";
        public IDMEEditor DMEEditor { get; set; }
        public PassedArgs ExecuteRule(PassedArgs args)
        {
            try
            {

                ObjectItem it = new ObjectItem();
                it.obj = DateTime.Now;
                it.Name = "GetDateTime";
               
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
        public GetDateTime()
        {

        }
    }
}
