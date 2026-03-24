using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.StreamandEvents
{
    /// <summary>Schema compatibility guarantee modes (aligned with Confluent SR model).</summary>
    public enum SchemaCompatibilityMode
    {
        /// <summary>Consumers on old version can read new schema (field additions only).</summary>
        Backward,

        /// <summary>Consumers on new version can read old schema.</summary>
        Forward,

        /// <summary>Both backward and forward.</summary>
        Full,

        /// <summary>No compatibility check enforced.</summary>
        None
    }

    /// <summary>
    /// Versioned schema artifact in the registry.
    /// Assigned a SchemaId on registration.
    /// </summary>
    public sealed class SchemaRegistryEntry
    {
        public string SchemaId { get; init; }
        public string EventType { get; init; }
        public int Version { get; init; }
        public string SchemaJson { get; init; }
        public string ContentType { get; init; } = "application/json";
        public SchemaCompatibilityMode CompatibilityMode { get; init; } = SchemaCompatibilityMode.Backward;
        public DateTime RegisteredAt { get; init; } = DateTime.UtcNow;
        public string RegisteredBy { get; init; }
        public bool IsDeprecated { get; init; }
    }

    /// <summary>Enforce topic naming, versioning, and lifecycle governance.</summary>
    public sealed class TopicNamingPolicy
    {
        /// <summary>Pattern template: use {domain}, {aggregate}, {event}, {version} tokens.</summary>
        public string NamePattern { get; init; } = "{domain}.{aggregate}.{event}.v{version}";

        public bool EnforceOnPublish { get; init; } = true;
        public bool EnforceOnSubscribe { get; init; } = true;

        /// <summary>Allowed domain names. Empty = unrestricted.</summary>
        public IReadOnlyList<string> AllowedDomains { get; init; } = Array.Empty<string>();

        public bool Validate(string topicName, out string reason)
        {
            if (string.IsNullOrWhiteSpace(topicName))
            {
                reason = "Topic name is null or empty.";
                return false;
            }

            // Minimal structural check: at least 3 segments
            var segments = topicName.Split('.');
            if (segments.Length < 3)
            {
                reason = $"Topic '{topicName}' has fewer than 3 dot-separated segments.";
                return false;
            }

            reason = null;
            return true;
        }
    }

    /// <summary>Lifecycle ownership metadata for a topic.</summary>
    public sealed class TopicLifecycleMetadata
    {
        public string TopicName { get; init; }
        public string OwnerService { get; init; }
        public string OwnerTeam { get; init; }
        public DateTime? DeprecatedAt { get; init; }
        public DateTime? SunsetAt { get; init; }
        public string MigrationTargetTopic { get; init; }
        public string ChangeNotes { get; init; }
    }
}
