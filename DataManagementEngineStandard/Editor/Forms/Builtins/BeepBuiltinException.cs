using System;

namespace TheTechIdea.Beep.Editor.Forms.Builtins
{
    /// <summary>
    /// Exception thrown by <see cref="IBeepBuiltins"/> when a built-in cannot
    /// complete. Carries an Oracle Forms-compatible error code
    /// (for example <c>FRM-41003</c>) so triggers and UI handlers can map
    /// failures to localized messages.
    /// </summary>
    public class BeepBuiltinException : Exception
    {
        public string ErrorCode { get; }
        public string BuiltinName { get; }
        public string? BlockName { get; }
        public string? ItemName { get; }

        public BeepBuiltinException(string errorCode, string builtinName, string message)
            : base(FormatMessage(errorCode, message))
        {
            ErrorCode = errorCode;
            BuiltinName = builtinName;
        }

        public BeepBuiltinException(string errorCode, string builtinName, string message, Exception innerException)
            : base(FormatMessage(errorCode, message), innerException)
        {
            ErrorCode = errorCode;
            BuiltinName = builtinName;
        }

        public BeepBuiltinException(string errorCode, string builtinName, string blockName, string message)
            : base(FormatMessage(errorCode, message))
        {
            ErrorCode = errorCode;
            BuiltinName = builtinName;
            BlockName = blockName;
        }

        public BeepBuiltinException(string errorCode, string builtinName, string blockName, string itemName, string message)
            : base(FormatMessage(errorCode, message))
        {
            ErrorCode = errorCode;
            BuiltinName = builtinName;
            BlockName = blockName;
            ItemName = itemName;
        }

        public BeepBuiltinException(string errorCode, string builtinName, string blockName, string itemName, string message, Exception innerException)
            : base(FormatMessage(errorCode, message), innerException)
        {
            ErrorCode = errorCode;
            BuiltinName = builtinName;
            BlockName = blockName;
            ItemName = itemName;
        }

        private static string FormatMessage(string errorCode, string message)
        {
            return string.IsNullOrWhiteSpace(errorCode) ? message : $"[{errorCode}] {message}";
        }
    }
}
