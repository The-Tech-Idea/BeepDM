using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TheTechIdea.Winforms.VIS
{
    public class WaitWndFun
    {
        Frm_Waiting loadingForm;
        Thread loadthread;
       
        public void Show()
        {
            loadthread = new Thread(new ThreadStart(LoadingProcessEx));
            loadthread.Start();
        }
      
        public void Show(Control parent)
        {
            loadthread = new Thread(new ParameterizedThreadStart(LoadingProcessEx));
            loadthread.Start(parent);
        }
        public void Close()
        {
            if (loadingForm != null)
            {
                loadingForm.BeginInvoke(new System.Threading.ThreadStart(loadingForm.CloseForm));
                loadingForm = null;
                loadthread = null;
            }
        }
        private void LoadingProcessEx()
        {
            loadingForm = new Frm_Waiting();
            loadingForm.ShowDialog();
        }
        private void LoadingProcessEx(object parent)
        {
            Form Cparent = parent as Form;
            loadingForm = new Frm_Waiting(Cparent);
            loadingForm.ShowDialog();
        }
        public void ChangeCaption(string newCaption)
        {
            // if (loadingForm != null)
            // {
            //  loadingForm.BeginInvoke(new System.Threading.ThreadStart(loadingForm.CloseForm));
            Thread.Sleep(100);
            loadingForm.ChangeCaption(newCaption);
           // }

        }
        public void AddComment(String comment)
        {
            Thread.Sleep(100);
            try
            {
                if (loadingForm != null)
                {
                    loadingForm.AddComment(comment);
                }
              
            }
            catch (Exception)
            {

                //throw;
            }
            
        }
    }
}
