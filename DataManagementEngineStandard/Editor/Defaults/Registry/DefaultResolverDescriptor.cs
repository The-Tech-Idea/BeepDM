using System;
using TheTechIdea.Beep.Editor.Defaults.Attributes;

namespace TheTechIdea.Beep.Editor.Defaults.Registry
{
    /// <summary>
    /// Immutable descriptor that pairs a <see cref="DefaultResolverAttribute"/> metadata
    /// snapshot with the concrete <see cref="Type"/> that implements
    /// <c>IDefaultValueResolver</c>.
    /// </summary>
    public sealed class DefaultResolverDescriptor
    {
        /// <summary>Metadata from the <see cref="DefaultResolverAttribute"/> on the class.</summary>
        public DefaultResolverAttribute Attribute { get; }

        /// <summary>The concrete type that implements <c>IDefaultValueResolver</c>.</summary>
        public Type ImplementationType { get; }

        public DefaultResolverDescriptor(DefaultResolverAttribute attribute, Type implementationType)
        {
            Attribute          = attribute          ?? throw new ArgumentNullException(nameof(attribute));
            ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
        }
    }
}
