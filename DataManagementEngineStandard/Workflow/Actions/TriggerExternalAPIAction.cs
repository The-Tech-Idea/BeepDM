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

namespace TheTechIdea.Beep.Workflow.Actions
{
    [Addin(Caption = "Trigger External API Action", Name = "TriggerExternalAPIAction", addinType = AddinType.Class)]
    public class TriggerExternalAPIAction : IWorkFlowAction
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public TriggerExternalAPIAction(IDMEEditor dmeEditor)
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
        public string ActionTypeName { get; set; } = "TriggerExternalAPIAction";
        public string Code { get; set; }
        public bool IsFinish { get; set; }
        public bool IsRunning { get; set; }
        public string ClassName { get; set; }
        public string Name { get; set; } = "TriggerExternalAPIAction";
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

        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token, Func<PassedArgs, object> actionToExecute)
        {
            var args = new PassedArgs { Messege = "API Trigger Started" };

            try
            {
                WorkFlowActionStarted?.Invoke(this, new WorkFlowEventArgs { Message = "Action Started", ActionName = Name });
                IsRunning = true;

                string url = GetParameterValue<string>("URL");
                string method = GetParameterValue<string>("Method")?.ToUpper() ?? "GET";
                string payload = GetParameterValue<string>("Payload");
                Dictionary<string, string> headers = GetParameterValue<Dictionary<string, string>>("Headers");

                if (string.IsNullOrEmpty(url))
                {
                    args.Messege = "URL cannot be null or empty.";
                    DMEEditor.AddLogMessage("API", args.Messege, DateTime.Now, -1, null, Errors.Failed);
                    return args;
                }

                // Trigger the API
                var apiTask = TriggerAPIAsync(url, method, payload, headers, progress, token);
                apiTask.Wait(token);

                args.Messege = "API Trigger Completed Successfully";
                WorkFlowActionEnded?.Invoke(this, new WorkFlowEventArgs { Message = "Action Completed", ActionName = Name });
            }
            catch (OperationCanceledException)
            {
                args.Messege = "API Trigger Canceled.";
            }
            catch (Exception ex)
            {
                args.Messege = $"Error triggering API: {ex.Message}";
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
            return new PassedArgs { Messege = "API Trigger Stopped." };
        }
        #endregion

        #region Helper Methods
        private async Task TriggerAPIAsync(
            string url,
            string method,
            string payload,
            Dictionary<string, string> headers,
            IProgress<PassedArgs> progress,
            CancellationToken token)
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = new HttpMethod(method)
            };

            // Add payload for POST/PUT
            if (method == "POST" || method == "PUT")
            {
                request.Content = new StringContent(payload ?? string.Empty, Encoding.UTF8, "application/json");
            }

            // Add headers
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            progress?.Report(new PassedArgs { Messege = $"Sending {method} request to {url}" });

            // Send request
            var response = await _httpClient.SendAsync(request, token);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                progress?.Report(new PassedArgs { Messege = $"API Response: {responseBody}" });
                DMEEditor.AddLogMessage("API", $"API Response: {responseBody}", DateTime.Now, -1, null, Errors.Ok);
            }
            else
            {
                DMEEditor.AddLogMessage("API", $"API Error: {response.StatusCode} - {responseBody}", DateTime.Now, -1, null, Errors.Failed);
            }
        }

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
