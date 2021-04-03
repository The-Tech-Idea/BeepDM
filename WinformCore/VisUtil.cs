

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Forms;

using TheTechIdea.Logger;
using TheTechIdea.DataManagment_Engine;
using System.Drawing;
using TheTechIdea.Util;

using TheTechIdea.Tools;
using TheTechIdea.DataManagment_Engine.Vis;


namespace TheTechIdea.Winforms.VIS
{
    public class VisUtil : IVisUtil

    {


        public IDMLogger Logger { get; set; }
        public IErrorsInfo Erinfo { get; set; }
     
        public IAssemblyHandler LLoader { get; set; }
        public PassedArgs Args { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public IControlEditor controlEditor { get; set; } 
        public ITree treeEditor { get; set; }
      
        //private TreeNode n;
        //private TreeNode nDV;
        private Control mdisplay = new Control();
        public Control DisplayPanel
        {
            get { return mdisplay; }
            set { mdisplay = value; }
        }
       // IWinFormAddin WinformCtl = null;

      //  public event EventHandler<PassedArgs> OnObjectSelected;
        //----------------------------------------------------
        #region Show Form or UserControl using Addin Interface
        private List<ObjectItem> CreateArgsParameterForVisUtil()
        {
            List<ObjectItem> objects = new List<ObjectItem>();
            ObjectItem v = new ObjectItem { Name = "VISUTIL", obj = (IVisUtil)this };
            objects.Add(v);
            return objects;
        }
        private PassedArgs CreateDefaultArgsForVisUtil()
        {

            PassedArgs E = new PassedArgs { Objects = CreateArgsParameterForVisUtil() };
            return E;
        }
        public IErrorsInfo CheckSystemEntryDataisSet()
        {
            Erinfo.Flag = Errors.Ok;

            try
            {

            }
            catch (System.Exception ex)
            {

                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
                Logger.WriteLog($"Error check main System entry variables ({ex.Message})");
            }
            return Erinfo;
        }
        private IErrorsInfo GetSystemEntryVariables()
        {
            Erinfo.Flag = Errors.Ok;

            try
            {


            }
            catch (System.Exception ex)
            {

                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
                Logger.WriteLog($"Error check main System entry variables ({ex.Message})");
            }
            return Erinfo;
        }
        public  IErrorsInfo PreSetupAddins()
        {


          
         //   IDM_Addin addin = null;

            //string objtype = "";
            //Boolean Show = true;
            //int cnt = 0;

            foreach (IDM_Addin ad in LLoader.AddIns)
            {

                Erinfo.Flag = Errors.Ok;
                //string otype = null;
                try
                {
                    Type type = ad.GetType();
                    if (type.IsSubclassOf(typeof(UserControl)))
                    {
                     
                        ad.ObjectName = type.Name;
                        ad.ObjectType = "UserControl";
                        
                    
                    
                    }
                    else
                    {
                       // Form uc = (Form)Activator.CreateInstance(type);
                        ad.ObjectName = type.Name;
                        ad.ObjectType = "Form";
                       
                      
                    }

                    try
                    {
                        var keyvalue=DMEEditor.Utilfunction.FunctionHierarchy.Where(c => (c.AddinName == ad.AddinName)).FirstOrDefault() ;
                        if (keyvalue != null)
                        {
                            keyvalue.ObjType = ad.ObjectType;
                        }
                    }
                    catch (Exception)
                    {


                    }

                }
                catch (System.Exception ex)
                {

                    Erinfo.Flag = Errors.Failed;
                    Erinfo.Ex = ex;
                    Logger.WriteLog($"Error check main System entry variables ({ex.Message})");
                }
            }

            return Erinfo;
        }
        public IErrorsInfo ShowMainDisplayForm()
        {
            Erinfo.Flag = Errors.Ok;
            Erinfo = CheckSystemEntryDataisSet();

            try
            {
                string[] args = { null, null, null };

                PassedArgs E = CreateDefaultArgsForVisUtil();

                IDM_Addin addinView = ShowFormFromAddin( DMEEditor.ConfigEditor.Config.SystemEntryFormName, DMEEditor, args, E);

               // Console.ReadLine();
            }
            catch (System.Exception ex)
            {

                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
                Logger.WriteLog($"Error Showing Main Application View ({ex.Message})");
            }
            return Erinfo;
        }
        public IErrorsInfo Run()
        {
            Erinfo.Flag = Errors.Ok;
            Erinfo = CheckSystemEntryDataisSet();

            try
            {
               

                if (Erinfo.Flag == Errors.Ok)
                {
                    ShowMainDisplayForm();
                }
                else
                {
                    Erinfo.Flag = Errors.Failed;

                  //  Logger.WriteLog($"Error Showing Main Application View (no error messege)");
                }


            }
            catch (System.Exception ex)
            {

                Erinfo.Flag = Errors.Failed;
                Erinfo.Ex = ex;
              //  Logger.WriteLog($"Error Showing Main Application View ({ex.Message})");
            }
            return Erinfo;
        }
        public IDM_Addin ShowUserControlInContainer(string usercontrolname, Control Container, IDMEEditor pDMEEditor, string[] args, PassedArgs e)
        {
           // string path = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + @"\Addin\";
            if (LLoader.AddIns.Where(x => x.ObjectName == usercontrolname).Any())
            {
                return ShowUserControlDialogOnControl( LLoader.AddIns.Where(c => c.ObjectName == usercontrolname).FirstOrDefault().DllName, usercontrolname, Container, pDMEEditor, args, e);
            }
            else
            {
                return null;
            }
                
        }
        public IDM_Addin ShowUserControlPopUp(string usercontrolname, IDMEEditor pDMEEditor, string[] args, PassedArgs e)
        {
            if (LLoader.AddIns.Where(x => x.ObjectName == usercontrolname).Any())
            {
                string path = LLoader.AddIns.Where(x => x.ObjectName == usercontrolname).FirstOrDefault().DllPath;

                return ShowUserControlDialog(path, LLoader.AddIns.Where(c => c.ObjectName == usercontrolname).FirstOrDefault().DllName, usercontrolname, pDMEEditor, args, e);
            }else
            {
              return  null;
            }
          
        }
        public IDM_Addin ShowFormFromAddin( string formname, IDMEEditor pDMEEditor, string[] args, PassedArgs e)
        {

            if (LLoader.AddIns.Where(x => x.ObjectName == formname).Any())
            {
                return ShowFormDialog(LLoader.AddIns.Where(c => c.ObjectName == formname).FirstOrDefault().DllName, formname, pDMEEditor, args, e);
            }
            else
            {
                return null;
            }
           

        }
        private IDM_Addin ShowUserControlDialogOnControl( string dllname, string formname, Control control, IDMEEditor pDMEEditor, string[] args, PassedArgs e)
        {
            control.Controls.Clear();
            Erinfo.Flag = Errors.Ok;
            //Form form = new Form();
           // var path = Path.Combine(dllpath, dllname);
            UserControl uc = new UserControl();
            IDM_Addin addin = null;
            if (e == null)
            {
                e = new PassedArgs();
            }
          
                try
                {
                    //Assembly assembly = Assembly.LoadFile(path);
                    //Type type = assembly.GetType(dllname + ".UserControls." + formname);
                    Type type = LLoader.AddIns.Where(c => c.ObjectName == formname).FirstOrDefault().GetType(); //dllname.Remove(dllname.IndexOf(".")) + ".Forms." + formname

                    uc = (UserControl)Activator.CreateInstance(type);
                    if (uc != null)
                    {
                        addin = (IDM_Addin)uc;
                    if (e.Objects == null)
                    {
                        e.Objects = new List<ObjectItem>();
                    }
                    e.Objects.AddRange( CreateArgsParameterForVisUtil());
                        addin.SetConfig(pDMEEditor, Logger, DMEEditor.Utilfunction, args, e, Erinfo);
                        control.Controls.Add(uc);
                        uc.Dock = DockStyle.Fill;

                    }
                    else
                    {
                        Erinfo.Flag = Errors.Failed;
                        Erinfo.Message = $"Error Could not Show UserControl { uc.Name}";
                        Logger.WriteLog(Erinfo.Message);
                    }


                }
                catch (Exception ex)
                {
                    Erinfo.Flag = Errors.Failed;
                    Erinfo.Message = $"Error While Loading Assembly " + ex.Message;
                    Logger.WriteLog($"Error While Loading Assembly " + ex.Message);

                }
           
            if (Erinfo.Flag == Errors.Ok)
            {
                control.Controls.Clear();
                control.Controls.Add(uc);
                uc.Dock = DockStyle.Fill;
            }
            return addin;
            //form.GetType().GetField("")
        }
        private IDM_Addin ShowFormDialog( string dllname, string formname, IDMEEditor pDMEEditor, string[] args, PassedArgs e)
        {
            Form form = null;
            IDM_Addin addin = null;
           // var path = Path.Combine(dllpath, dllname);
            Erinfo.Flag = Errors.Ok;
            if (e == null)
            {
                e = new PassedArgs();
            }
          

                try
                {
                    // Assembly assembly = Assembly.LoadFile(path);
                    Type type = LLoader.AddIns.Where(c => c.ObjectName == formname).FirstOrDefault().GetType(); //dllname.Remove(dllname.IndexOf(".")) + ".Forms." + formname
                    form = (Form)Activator.CreateInstance(type);
                    if (form != null)
                    {
                        addin = (IDM_Addin)form;
                    if (e.Objects == null)
                    {
                        e.Objects = new List<ObjectItem>();
                    }
                    e.Objects.AddRange(CreateArgsParameterForVisUtil());
                        addin.SetConfig(pDMEEditor, Logger, DMEEditor.Utilfunction, args, e, Erinfo);
                        form.Text = addin.AddinName;


                    }
                    else
                    {
                        Erinfo.Flag = Errors.Failed;
                        Erinfo.Message = $"Error Could not Show Form { form.Name}";
                  //      Logger.WriteLog(Erinfo.Message);
                    };
                }
                catch (Exception ex)
                {
                    Erinfo.Flag = Errors.Failed;
                    Erinfo.Message = ex.Message;
              //      Logger.WriteLog($"Error While Loading Assembly {path} : {ex.Message}");

                }
           
           
                form.ShowDialog();
   

            return addin;
            //form.GetType().GetField("")
        }
        private IDM_Addin ShowUserControlDialog(string dllpath, string dllname, string formname, IDMEEditor pDMEEditor, string[] args, PassedArgs e)
        {
            Erinfo.Flag = Errors.Ok;
            Form form = new Form();
            var path = Path.Combine(dllpath, dllname);
            IDM_Addin addin = null;
            if (e == null)
            {
                e = new PassedArgs();
            }
          
                try
                {
                    // Assembly assembly = Assembly.LoadFile(path);
                    //Type type = assembly.GetType(dllname + ".UserControls." + formname);
                    Type type = LLoader.AddIns.Where(c => c.ObjectName == formname).FirstOrDefault().GetType();
                    UserControl uc = (UserControl)Activator.CreateInstance(type);
                    if (uc != null)
                    {
                        addin = (IDM_Addin)uc;
                    if (e.Objects == null)
                    {
                        e.Objects = new List<ObjectItem>();
                    }
                        e.Objects.AddRange(CreateArgsParameterForVisUtil());
                     

                        form.Text = addin.AddinName;
                        addin.SetConfig(pDMEEditor, Logger, DMEEditor.Utilfunction, args, e, Erinfo);
                        form.Controls.Add(uc);
                        form.Width = uc.Width + 50;
                        form.Height = uc.Height + 50;
                        uc.Dock = DockStyle.Fill;
                        form.ShowDialog();
                      


                    }
                    else
                    {
                        Erinfo.Flag = Errors.Failed;
                        Erinfo.Message = $"Error Could not Show UserControl { uc.Name}";
                        Logger.WriteLog(Erinfo.Message);
                    }


                }
                catch (Exception ex)
                {
                    Erinfo.Flag = Errors.Failed;
                    Erinfo.Message = $"Error While Loading Assembly {path} :{ex.Message}";
                    Logger.WriteLog($"Error While Loading Assembly {path} :{ex.Message}");

                }
            
           




            return addin;
            //form.GetType().GetField("")
        }
        #endregion
      
   
        //-----------------------------------------------------

        public VisUtil(IDMEEditor pDMEEditor, IDMLogger logger, IErrorsInfo per, IAssemblyHandler pLLoader)
        {
            DMEEditor = pDMEEditor;
            Logger = logger;
            LLoader = pLLoader;
            Erinfo = per;

        }

        public class DatasourceCategoryDataItem
        {
            public DatasourceCategory Value { get; set; }
            public string Text { get; set; }
        }
    }
}
