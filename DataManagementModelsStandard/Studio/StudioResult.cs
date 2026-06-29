// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Studio;

/// <summary>
/// Result type for all Beep Studio engine calls. Every public mutation or query on
/// <see cref="TheTechIdea.Beep.Studio.Contracts.IStudioService"/> returns a
/// <see cref="StudioResult{T}"/> instead of throwing for business-level errors.
/// Throwing is reserved for programmer errors (null arguments, contract violations).
/// </summary>
/// <remarks>
/// The struct is a <c>readonly record struct</c> so it can be returned by value
/// without an allocation on the hot path. The shape mirrors
/// <c>Microsoft.Extensions.Logging</c>-style error envelopes but is engine-agnostic.
/// </remarks>
/// <typeparam name="T">The success payload type.</typeparam>
public readonly record struct StudioResult<T>(
    bool IsSuccess,
    T? Value,
    StudioError Error)
{
    /// <summary>True when the call succeeded; the <see cref="Value"/> is the payload.</summary>
    public bool IsSuccess { get; } = IsSuccess;

    /// <summary>The success payload. <c>null</c> when <see cref="IsSuccess"/> is false.</summary>
    public T? Value { get; } = Value;

    /// <summary>The error envelope. <see cref="StudioError.None"/> when the call succeeded.</summary>
    public StudioError Error { get; } = Error;

    /// <summary>Create a successful result with the supplied payload.</summary>
    public static StudioResult<T> Ok(T value) => new(true, value, StudioError.None);

    /// <summary>Create a failed result with the supplied error code and message.</summary>
    public static StudioResult<T> Fail(StudioErrorCode code, string message, Exception? ex = null)
        => new(false, default, new StudioError(code, message, ex, null));

    /// <summary>Create a failed result with the supplied error code, message, and structured details.</summary>
    public static StudioResult<T> Fail(
        StudioErrorCode code,
        string message,
        Exception? ex,
        IReadOnlyDictionary<string, object?>? details)
        => new(false, default, new StudioError(code, message, ex, details));

    /// <summary>Create a failed result from a pre-built <see cref="StudioError"/>.</summary>
    public static StudioResult<T> Fail(StudioError error) => new(false, default, error);

    /// <summary>
    /// Implicit conversion from <typeparamref name="T"/> to a successful <see cref="StudioResult{T}"/>.
    /// Lets call sites do <c>return someValue;</c> when the success path is the only path.
    /// </summary>
    public static implicit operator StudioResult<T>(T value) => Ok(value);

    /// <summary>Match on success/failure and produce a non-Result value.</summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<StudioError, TResult> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Error);
}
