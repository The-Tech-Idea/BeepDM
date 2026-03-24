using System;
using TheTechIdea.Beep.FileManager.Attributes;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.FileManager.Registry
{
    /// <summary>
    /// Immutable descriptor that pairs <see cref="FileReaderAttribute"/> metadata with the
    /// concrete <see cref="Type"/> that implements <c>IFileFormatReader</c>.
    /// </summary>
    public sealed class FileReaderDescriptor
    {
        /// <summary>Metadata from the <see cref="FileReaderAttribute"/> on the class.</summary>
        public FileReaderAttribute Attribute { get; }

        /// <summary>The concrete type that implements <c>IFileFormatReader</c>.</summary>
        public Type ImplementationType { get; }

        /// <summary>The format this reader handles.</summary>
        public DataSourceType FormatType { get; }

        public FileReaderDescriptor(FileReaderAttribute attribute, Type implementationType)
        {
            Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
            FormatType = attribute.FormatType;
        }
    }
}
