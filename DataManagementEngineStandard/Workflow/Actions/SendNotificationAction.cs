using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.ConfigUtil;
using System.Net.Mail;

namespace TheTechIdea.Beep.Workflow.Actions
{
    [Addin(Caption = "Send Notification Action", Name = "SendNotificationAction", addinType = AddinType.Class)]
    public class SendNotificationAction : IWorkFlowAction
    {
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public SendNotificationAction(IDMEEditor dmeEditor)
        {
            DMEEditor = dmeEditor;
            Id = Guid.NewGuid().ToString();
            NextAction = new List<IWorkFlowAction>();
            InParameters = new List<IPassedArgs>();
            OutParameters = new List<IPassedArgs>();
            Rules = new List<IWorkFlowRule>();
        }

        #region Properties
        public IWorkFlowAction PrevAction { get; set; }
        public List<IWorkFlowAction> NextAction { get; set; }
        public List<IPassedArgs> InParameters { get; set; }
        public List<IPassedArgs> OutParameters { get; set; }
        public List<IWorkFlowRule> Rules { get; set; }
        public string Id { get; set; }
        public string ActionTypeName { get; set; } = "SendNotificationAction";
        public string Code { get; set; }
        public bool IsFinish { get; set; }
        public bool IsRunning { get; set; }
        public string ClassName { get; set; }
        public string Name { get; set; } = "SendNotificationAction";
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionStarted;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionEnded;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionRunning;
        public IDMEEditor DMEEditor { get; }
        #endregion

        #region Perform Action
        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token)
        {
            return PerformAction(progress, token, null);
        }

        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token, Func<PassedArgs> actionToExecute)
        {
            var args = new PassedArgs { Messege = "Notification Trigger Started" };

            try
            {
                WorkFlowActionStarted?.Invoke(this, new WorkFlowEventArgs { Message = "Action Started", ActionName = Name });
                IsRunning = true;

                // Extract parameters
                string recipientList = GetParameterValue<string>("RecipientList");
                string messageTemplate = GetParameterValue<string>("MessageTemplate");
                string subject = GetParameterValue<string>("Subject");
                string notificationType = GetParameterValue<string>("NotificationType")?.ToLower();

                if (string.IsNullOrEmpty(recipientList) || string.IsNullOrEmpty(messageTemplate))
                {
                    args.Messege = "Recipient list or message template cannot be empty.";
                    DMEEditor.AddLogMessage("Notification", args.Messege, DateTime.Now, -1, null, Errors.Failed);
                    return args;
                }

                // Choose the notification type
                switch (notificationType)
                {
                    case "email":
                        SendEmailNotification(recipientList, subject, messageTemplate);
                        break;

                    case "sms":
                        SendSMSNotification(recipientList, messageTemplate);
                        break;

                    case "system":
                        SendSystemNotification(recipientList, messageTemplate);
                        break;

                    default:
                        args.Messege = "Invalid Notification Type. Supported types: email, sms, system.";
                        DMEEditor.AddLogMessage("Notification", args.Messege, DateTime.Now, -1, null, Errors.Failed);
                        return args;
                }

                args.Messege = "Notification Sent Successfully.";
                WorkFlowActionEnded?.Invoke(this, new WorkFlowEventArgs { Message = "Action Completed", ActionName = Name });
            }
            catch (OperationCanceledException)
            {
                args.Messege = "Notification Trigger Canceled.";
            }
            catch (Exception ex)
            {
                args.Messege = $"Error Sending Notification: {ex.Message}";
            }
            finally
            {
                IsRunning = false;
                IsFinish = true;
            }

            return args;
        }

        public PassedArgs StopAction()
        {
            _cancellationTokenSource.Cancel();
            return new PassedArgs { Messege = "Notification Sending Stopped." };
        }
        #endregion

        #region Notification Methods
        private void SendEmailNotification(string recipients, string subject, string messageBody)
        {
            try
            {
                using (var mailMessage = new MailMessage())
                {
                    foreach (var email in recipients.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        mailMessage.To.Add(email.Trim());
                    }

                    mailMessage.Subject = subject;
                    mailMessage.Body = messageBody;
                    mailMessage.IsBodyHtml = true;

                    using (var smtpClient = new SmtpClient("smtp.example.com")) // Update with actual SMTP details
                    {
                        smtpClient.Send(mailMessage);
                        DMEEditor.AddLogMessage("Notification", $"Email sent to: {recipients}", DateTime.Now, -1, null, Errors.Ok);
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Notification", $"Failed to send email: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                throw;
            }
        }

        private void SendSMSNotification(string recipients, string messageBody)
        {
            // Placeholder logic for sending SMS
            DMEEditor.AddLogMessage("Notification", $"SMS sent to: {recipients} - Message: {messageBody}", DateTime.Now, -1, null, Errors.Ok);
        }

        private void SendSystemNotification(string recipients, string messageBody)
        {
            // Placeholder logic for sending system notifications
            DMEEditor.AddLogMessage("Notification", $"System notification sent to: {recipients} - Message: {messageBody}", DateTime.Now, -1, null, Errors.Ok);
        }
        #endregion

        #region Helper Methods
        private T GetParameterValue<T>(string parameterName)
        {
            foreach (var param in InParameters)
            {
                if (param.ParameterString1 == parameterName && param.ReturnData is T value)
                    return value;
            }
            return default;
        }
        #endregion
    }
}
