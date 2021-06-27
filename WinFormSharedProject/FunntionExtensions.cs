using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.DataManagment_Engine.Addin;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Util;


namespace TheTechIdea.DataManagment_Engine.Vis
{
    public  class FunntionExtensions : IFunctionExtension
    {
        public FunntionExtensions(IDMEEditor pDMEEditor)
        {
            DMEEditor = pDMEEditor ?? throw new ArgumentNullException(nameof(pDMEEditor));
        }
        public  IDMEEditor DMEEditor { get; set; }

        [CommandAttribute(Caption = "Copy Entities", Click =true,iconimage ="copyentities.ico",PointType= EnumPointType.DataPoint)]
        public IErrorsInfo CopyEntities(IPassedArgs Passedarguments)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            IDataSource DataSource = null;
            //   DMEEditor.Logger.WriteLog($"Filling Database Entites ) ");
            try
            {
                List<EntityStructure> ents = new List<EntityStructure>();
               
                string[] args = new string[] { Passedarguments.DatasourceName, DataSource.Dataconnection.ConnectionProp.SchemaName, null };


                if (DataSource != null)
                {
                    DataSource.GetEntitesList();
                   
                
                    //        Visutil.ShowUserControlPopUp("uc_datasourceDefaults", DMEEditor, args, Passedarguments);
                }
                else
                {
                    DMEEditor.AddLogMessage("Fail", "Could not get DataSource", DateTime.Now, -1, null, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.Logger.WriteLog($"Error in Filling Database Entites ({ex.Message}) ");
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
            }
            return DMEEditor.ErrorObject;
        }
        [CommandAttribute(Caption = "Paste Entities", Click = true, iconimage = "pasteentities.ico", PointType = EnumPointType.DataPoint)]
        public void PasteEntities(IPassedArgs Passedarguments)
        {

        }
        [CommandAttribute(Caption = "Copy Default", Click = true, iconimage = "copydefault.ico", PointType = EnumPointType.DataPoint)]
        public void CopyDefault(IPassedArgs Passedarguments)
        {

        }
        [CommandAttribute(Caption = "Paste Default", Click = true, iconimage = "pastedefault.ico", PointType = EnumPointType.DataPoint)]
        public void PasteDefault(IPassedArgs Passedarguments)
        {

        }
        [CommandAttribute(Caption = "Refresh", Click = true, iconimage = "refresg.ico", PointType = EnumPointType.DataPoint)]
        public void Refresh(IPassedArgs Passedarguments)
        {

        }
    }
}
