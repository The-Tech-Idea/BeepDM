using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.Defaults.Helpers;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Defaults.Interfaces;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    internal sealed class RequiredResolverRegistry
    {
        private readonly IDMEEditor _editor;
        private readonly DefaultValueResolverManager _manager;
        private readonly IDefaultValueResolver[] _ordered;

        internal RequiredResolverRegistry(IDMEEditor editor, DefaultValueResolverManager manager,
            IDefaultValueResolver[] ordered)
        {
            _editor = editor;
            _manager = manager;
            _ordered = ordered;
        }

        internal bool IsOwnedBy(IDMEEditor editor) => ReferenceEquals(_editor, editor);
        internal IDefaultValueResolver Select(string rule)
        {
            var token = RequiredDefaultResolution.Current?.Token ?? CancellationToken.None;
            foreach (var resolver in _ordered)
            {
                token.ThrowIfCancellationRequested();
                if (RequiredDefaultResolution.Current?.Failed == true) return null;
                var handles = resolver.CanHandle(rule);
                token.ThrowIfCancellationRequested();
                if (RequiredDefaultResolution.Current?.Failed == true) return null;
                if (handles) return resolver;
            }
            return null;
        }
        internal object Resolve(string rule, IPassedArgs parameters, CancellationToken token) =>
            _manager.ResolveRequired(rule, parameters, token, this);
    }

    // Flows with an admitted operation, without turning source/provider callbacks into required rules.
    internal sealed class RequiredResolverContext : IDisposable
    {
        private static readonly AsyncLocal<RequiredResolverContext> Slot = new();
        private readonly RequiredResolverContext _previous;
        private readonly RequiredResolverRegistry _registry;
        private readonly string _dataSourceName;
        private readonly Dictionary<string, DefaultValue> _definitions;
        internal static RequiredResolverRegistry Current => Slot.Value?._registry;

        internal RequiredResolverContext(RequiredResolverRegistry registry, string dataSourceName,
            List<DefaultValue> definitions)
        {
            _previous = Slot.Value;
            _registry = registry;
            _dataSourceName = dataSourceName;
            _definitions = DefaultValueHelper.CaptureRequired(definitions)
                .ToDictionary(value => value.PropertyName, StringComparer.OrdinalIgnoreCase);
            Slot.Value = this;
        }

        internal static DefaultValue GetDefinitionRequired(IDMEEditor editor, string dataSourceName, string fieldName)
        {
            var scope = Slot.Value;
            if (scope?._registry == null || !scope._registry.IsOwnedBy(editor) ||
                !string.Equals(scope._dataSourceName, dataSourceName, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(fieldName) || !scope._definitions.TryGetValue(fieldName, out var definition))
            {
                RequiredDefaultResolution.Report(failure: true);
                throw new ImportTransformationException(ImportTransformationStage.Defaults);
            }
            return DefaultValueHelper.CaptureRequired(new List<DefaultValue> { definition })[0];
        }

        public void Dispose() => Slot.Value = _previous;
    }
}
