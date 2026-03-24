using System;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Protobuf descriptor provider ──────────────────────────────────────────

    /// <summary>
    /// Provides Protobuf descriptor information for CLR message types.
    /// Implementations may wrap <c>Google.Protobuf.Reflection.MessageDescriptor</c> or
    /// <c>ProtoBuf.Meta.RuntimeTypeModel</c> — both are kept out of this models-layer interface
    /// to avoid forcing a Protobuf SDK dependency.
    /// </summary>
    public interface IProtobufDescriptorProvider
    {
        /// <summary>
        /// Returns a human-readable .proto definition string for <paramref name="messageType"/>.
        /// </summary>
        string GetDescriptor(Type messageType);

        /// <summary>
        /// Returns the binary <c>FileDescriptorProto</c> bytes for <paramref name="messageType"/>,
        /// suitable for uploading to a schema registry that accepts binary descriptors.
        /// </summary>
        byte[] GetFileDescriptorBytes(Type messageType);
    }

    // ── Attribute ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Maps a Protobuf message CLR type to a schema registry subject name.
    /// Attach to protobuf message classes so the registry interceptor can auto-resolve
    /// the subject string without runtime reflection on the type name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class ProtoContractSubjectAttribute : Attribute
    {
        /// <summary>
        /// Schema registry subject name, e.g. <c>my-topic-value</c>.
        /// </summary>
        public string Subject { get; }

        /// <summary>
        /// Initialises the attribute with a subject name.
        /// </summary>
        public ProtoContractSubjectAttribute(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException("Subject must not be null or empty.", nameof(subject));

            Subject = subject;
        }
    }
}
