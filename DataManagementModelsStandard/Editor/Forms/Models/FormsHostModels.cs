using System;

namespace TheTechIdea.Beep.Editor.Forms.Models;

/// <summary>
/// One query-by-example value supplied by a platform host.
/// </summary>
public sealed record QueryCriterion(
    object? Value,
    QueryOperator Operator = QueryOperator.Equals,
    bool IsEnabled = true);

/// <summary>
/// Platform-neutral message event relayed from the Forms engine.
/// </summary>
public sealed class FormsHostMessageEventArgs(
    string blockName,
    string message,
    MessageLevel level) : EventArgs
{
    public string BlockName { get; } = blockName;
    public string Message { get; } = message;
    public MessageLevel Level { get; } = level;
}

/// <summary>
/// Platform-neutral timer event relayed from the Forms engine.
/// </summary>
public sealed class FormsHostTimerEventArgs(
    string timerName,
    int fireCount,
    DateTime firedAt) : EventArgs
{
    public string TimerName { get; } = timerName;
    public int FireCount { get; } = fireCount;
    public DateTime FiredAt { get; } = firedAt;
}

/// <summary>
/// Platform-neutral inter-form message relayed from the Forms engine.
/// </summary>
public sealed class FormsHostFormMessageEventArgs(FormMessage message) : EventArgs
{
    public FormMessage Message { get; } =
        message ?? throw new ArgumentNullException(nameof(message));
    public string MessageType => Message.MessageType;
    public object? Payload => Message.Payload;
}
