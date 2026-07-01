using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.Installer.Steps
{
    /// <summary>Adds/removes Windows Firewall rules for the application.</summary>
    public class FirewallStep : ISetupStep
    {
        public string StepId => "installer.firewall";
        public string StepName => "Configure firewall";
        public string Description => "Adds Windows Firewall rules.";
        public IReadOnlyList<string> DependsOn { get; }

        private readonly bool _isUninstall;

        public FirewallStep(bool isUninstall = false, string? dependsOn = null)
        {
            _isUninstall = isUninstall;
            DependsOn = dependsOn != null ? new List<string> { dependsOn } : Array.Empty<string>();
        }

        public bool CanSkip(SetupContext context) => false;

        public IErrorsInfo Validate(SetupContext context) => StepErrorHelpers.Ok("Validated.");

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var rules = context.TryGetProperty<List<FirewallRule>>("FirewallRules");
            if (rules == null || rules.Count == 0)
                return StepErrorHelpers.Ok("No firewall rules configured.");

            foreach (var rule in rules)
            {
                progress?.Report(new PassedArgs { Messege = _isUninstall ? $"Removing rule: {rule.Name}" : $"Adding rule: {rule.Name}" });

                if (_isUninstall)
                    InstallHelpers.RemoveFirewallRule(rule.Name);
                else
                    InstallHelpers.AddFirewallRule(rule.Name, rule.ProgramPath, rule.Port, rule.Protocol);
            }

            return StepErrorHelpers.Ok($"{(_isUninstall ? "Removed" : "Added")} {rules.Count} firewall rules.");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));
    }

    public class FirewallRule
    {
        public string Name { get; set; } = "";
        public string ProgramPath { get; set; } = "";
        public string Port { get; set; } = "";
        public string Protocol { get; set; } = "TCP";
    }
}
