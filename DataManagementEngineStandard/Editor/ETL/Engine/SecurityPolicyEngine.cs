using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Engine
{
    /// <summary>
    /// Pre-run and post-run security enforcement for pipeline execution.
    /// Validates caller permissions against <see cref="PipelineDefinition.RequiredPermissions"/>
    /// and data-classification rules before allowing a run to proceed.
    /// </summary>
    public class SecurityPolicyEngine
    {
        /// <summary>
        /// Evaluates all security policies before a pipeline run begins.
        /// Returns an empty list when the caller is authorised.
        /// </summary>
        public Task<IReadOnlyList<SecurityViolation>> ValidatePreRunAsync(
            PipelineDefinition definition,
            SecurityContext? caller)
        {
            var violations = new List<SecurityViolation>();

            // ── 1. Permission check ─────────────────────────────────────
            if (definition.RequiredPermissions.Count > 0)
            {
                if (caller == null)
                {
                    violations.Add(new SecurityViolation
                    {
                        Code    = "SEC-001",
                        Message = "Pipeline requires authentication but no SecurityContext was provided.",
                        Severity = ViolationSeverity.Error
                    });
                }
                else
                {
                    foreach (var perm in definition.RequiredPermissions)
                    {
                        if (!caller.HasPermission(perm))
                        {
                            violations.Add(new SecurityViolation
                            {
                                Code    = $"SEC-002",
                                Message = $"Caller '{caller.UserName}' lacks required permission '{perm}'.",
                                Severity = ViolationSeverity.Error
                            });
                        }
                    }
                }
            }

            // ── 2. Data-classification guardrails ───────────────────────
            if (definition.DataClassification == DataClassification.Restricted)
            {
                if (definition.SensitiveFields.Count == 0)
                {
                    violations.Add(new SecurityViolation
                    {
                        Code    = "SEC-003",
                        Message = "Pipeline classified as Restricted but no SensitiveFields are declared.",
                        Severity = ViolationSeverity.Warning
                    });
                }

                if (definition.MaskingStrategy == MaskingStrategy.None)
                {
                    violations.Add(new SecurityViolation
                    {
                        Code    = "SEC-004",
                        Message = "Pipeline classified as Restricted but MaskingStrategy is None.",
                        Severity = ViolationSeverity.Warning
                    });
                }
            }

            // ── 3. Compliance tag validation ────────────────────────────
            if (definition.ComplianceTags.Count > 0 && string.IsNullOrWhiteSpace(definition.Owner))
            {
                violations.Add(new SecurityViolation
                {
                    Code    = "SEC-005",
                    Message = "Pipeline has compliance tags but no Owner is assigned.",
                    Severity = ViolationSeverity.Warning
                });
            }

            return Task.FromResult<IReadOnlyList<SecurityViolation>>(violations);
        }

        /// <summary>
        /// Determines whether any violation is blocking (Error severity).
        /// </summary>
        public static bool HasBlockingViolations(IReadOnlyList<SecurityViolation> violations)
        {
            for (int i = 0; i < violations.Count; i++)
                if (violations[i].Severity == ViolationSeverity.Error)
                    return true;
            return false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Describes a single security policy violation detected pre-run.</summary>
    public class SecurityViolation
    {
        /// <summary>Machine-readable code (e.g. "SEC-001").</summary>
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public ViolationSeverity Severity { get; set; } = ViolationSeverity.Error;
    }

    /// <summary>Severity of a security violation.</summary>
    public enum ViolationSeverity
    {
        /// <summary>Informational — logged but does not block execution.</summary>
        Info,
        /// <summary>Warning — logged with alert, execution may proceed.</summary>
        Warning,
        /// <summary>Error — blocks pipeline execution.</summary>
        Error
    }
}
