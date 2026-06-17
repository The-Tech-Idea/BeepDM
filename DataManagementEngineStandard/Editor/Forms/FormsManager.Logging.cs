using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    partial class FormsManager
    {
        private ILogger<FormsManager> _logger = NullLogger<FormsManager>.Instance;

        internal ILogger<FormsManager> Logger
        {
            get => _logger;
            set => _logger = value ?? NullLogger<FormsManager>.Instance;
        }

        internal IDisposable? BeginScope(string blockName, string operation)
        {
            var scopeState = new Dictionary<string, object?>
            {
                ["OperationId"] = Guid.NewGuid(),
                ["BlockName"] = blockName,
                ["Operation"] = operation
            };
            return _logger.BeginScope(scopeState);
        }

        private void LogOperationStructured(string message, string? blockName = null, string? operation = null)
        {
            var args = !string.IsNullOrWhiteSpace(blockName)
                ? new object[] { blockName, message }
                : new object[] { message };

            var template = !string.IsNullOrWhiteSpace(blockName)
                ? "[{BlockName}] {Message}"
                : "{Message}";

            _logger.LogInformation(template, args);
        }

        private void LogErrorStructured(string message, Exception? ex = null, string? blockName = null)
        {
            var args = !string.IsNullOrWhiteSpace(blockName)
                ? new object[] { blockName, message }
                : new object[] { message };

            var template = !string.IsNullOrWhiteSpace(blockName)
                ? "[{BlockName}] {Message}"
                : "{Message}";

            _logger.LogError(ex, template, args);
        }

        private void LogWarningStructured(string message, string? blockName = null)
        {
            var args = !string.IsNullOrWhiteSpace(blockName)
                ? new object[] { blockName, message }
                : new object[] { message };

            var template = !string.IsNullOrWhiteSpace(blockName)
                ? "[{BlockName}] {Message}"
                : "{Message}";

            _logger.LogWarning(template, args);
        }

        private void LogDebugStructured(string message, string? blockName = null)
        {
            var args = !string.IsNullOrWhiteSpace(blockName)
                ? new object[] { blockName, message }
                : new object[] { message };

            var template = !string.IsNullOrWhiteSpace(blockName)
                ? "[{BlockName}] {Message}"
                : "{Message}";

            _logger.LogDebug(template, args);
        }
    }
}
