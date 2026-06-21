using System;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.Forms.Helpers;

public static class MessageClassifier
{
    public static BeepMessageSeverity ClassifyCommandResult(IErrorsInfo? result, BeepMessageSeverity successSeverity = BeepMessageSeverity.Success)
    {
        if (result == null) return BeepMessageSeverity.Error;
        return result.Flag switch
        {
            Errors.Ok => ClassifyFromText(result.Message, successSeverity),
            Errors.Warning => BeepMessageSeverity.Warning,
            Errors.Information => BeepMessageSeverity.Info,
            Errors.Critical => BeepMessageSeverity.Error,
            Errors.Exception => BeepMessageSeverity.Error,
            Errors.Error => BeepMessageSeverity.Error,
            Errors.Fatal => BeepMessageSeverity.Error,
            _ => ClassifyFromText(result.Message, BeepMessageSeverity.Error)
        };
    }

    public static BeepMessageSeverity ClassifyFromText(string? message, BeepMessageSeverity defaultSeverity)
    {
        if (string.IsNullOrWhiteSpace(message)) return defaultSeverity;
        if (ContainsAny(message, StringComparison.OrdinalIgnoreCase,
                "cancelled", "canceled", "blocked", "not allowed", "not permitted",
                "validation failed", "duplicate", "no changes", "must be in query mode",
                "already in query mode", "cannot enter query mode"))
            return BeepMessageSeverity.Warning;
        if (ContainsAny(message, StringComparison.OrdinalIgnoreCase,
                "error", "exception", "fatal", "critical", "failed"))
            return BeepMessageSeverity.Error;
        if (ContainsAny(message, StringComparison.OrdinalIgnoreCase, "warning"))
            return BeepMessageSeverity.Warning;
        if (ContainsAny(message, StringComparison.OrdinalIgnoreCase,
                "success", "completed", "committed", "rolled back", "executed successfully",
                "entered query mode", "navigated", "switched to block"))
            return defaultSeverity;
        if (ContainsAny(message, StringComparison.OrdinalIgnoreCase, "info", "ready"))
            return BeepMessageSeverity.Info;
        return defaultSeverity;
    }

    public static BeepMessageSeverity ClassifyFromMessageType(string? messageType)
    {
        if (string.IsNullOrWhiteSpace(messageType)) return BeepMessageSeverity.Info;
        if (messageType.Contains("error", StringComparison.OrdinalIgnoreCase) ||
            messageType.Contains("fail", StringComparison.OrdinalIgnoreCase))
            return BeepMessageSeverity.Error;
        if (messageType.Contains("warn", StringComparison.OrdinalIgnoreCase))
            return BeepMessageSeverity.Warning;
        if (Regex.IsMatch(messageType, @"\bsuccess\b", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(messageType, @"\bok\b", RegexOptions.IgnoreCase))
            return BeepMessageSeverity.Success;
        return BeepMessageSeverity.Info;
    }

    public static BeepMessageSeverity MapMessageLevel(MessageLevel level) => level switch
    {
        MessageLevel.Success => BeepMessageSeverity.Success,
        MessageLevel.Warning => BeepMessageSeverity.Warning,
        MessageLevel.Error => BeepMessageSeverity.Error,
        _ => BeepMessageSeverity.Info
    };

    public static BeepMessageSeverity MapErrorSeverity(ErrorSeverity severity) => severity switch
    {
        ErrorSeverity.Critical => BeepMessageSeverity.Error,
        ErrorSeverity.Error => BeepMessageSeverity.Error,
        ErrorSeverity.Warning => BeepMessageSeverity.Warning,
        ErrorSeverity.Info => BeepMessageSeverity.Info,
        _ => BeepMessageSeverity.Info
    };

    public static BeepMessageSeverity MapAlertSeverity(AlertStyle style, AlertResult result)
    {
        if (result == AlertResult.None) return BeepMessageSeverity.Warning;
        return style switch
        {
            AlertStyle.Stop => BeepMessageSeverity.Error,
            AlertStyle.Caution => BeepMessageSeverity.Warning,
            _ => BeepMessageSeverity.Info
        };
    }

    public static bool IsNeutralStatus(string? status) =>
        string.IsNullOrWhiteSpace(status) ||
        string.Equals(status.Trim(), "Ready", StringComparison.OrdinalIgnoreCase);

    public static BeepMessageSeverity ResolveTriggerChainSeverity(TriggerChainCompletedEventArgs e)
    {
        if (e.FailureCount > 0) return BeepMessageSeverity.Error;
        if (e.WasCancelled || e.SkippedCount > 0)
            return e.WasCancelled ? BeepMessageSeverity.Warning : BeepMessageSeverity.Info;
        return e.SuccessCount > 0 ? BeepMessageSeverity.Success : BeepMessageSeverity.Info;
    }

    private static bool ContainsAny(string message, StringComparison comparison, params string[] tokens)
    {
        foreach (string token in tokens)
            if (message.Contains(token, comparison))
                return true;
        return false;
    }
}
