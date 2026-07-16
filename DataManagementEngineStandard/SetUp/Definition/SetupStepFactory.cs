using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TheTechIdea.Beep.SetUp.Seeding;
using TheTechIdea.Beep.SetUp.Steps;

namespace TheTechIdea.Beep.SetUp.Definition
{
    /// <summary>
    /// Default <see cref="ISetupStepFactory"/>, pre-registered with the six built-in step types.
    /// </summary>
    public sealed class SetupStepFactory : ISetupStepFactory
    {
        /// <summary>Type keys for the built-in steps.</summary>
        public static class TypeKeys
        {
            public const string DriverProvision = "driver-provision";
            public const string ConnectionConfig = "connection-config";
            public const string SchemaSetup = "schema-setup";
            public const string DefaultsSetup = "defaults-setup";
            public const string Seeding = "seeding";
            public const string DataImport = "data-import";
        }

        // Shared settings so options written by ISetupStep.SerializeOptions bind here, and so a
        // hand-authored "databaseType": "SqlLite" resolves. See SetupJson.
        private static readonly JsonSerializerOptions OptionsRead = SetupJson.Options;

        private readonly ConcurrentDictionary<string, Func<JsonElement?, ISetupStep>> _factories =
            new(StringComparer.Ordinal);

        private readonly ISeederRegistry _seeders;

        /// <param name="seeders">
        /// Injected from DI, never from a definition file — <see cref="SeedingStepOptions.Registry"/>
        /// is a live object graph and is <c>[JsonIgnore]</c>d for exactly that reason.
        /// </param>
        public SetupStepFactory(ISeederRegistry seeders = null)
        {
            _seeders = seeders;
            RegisterBuiltIns();
        }

        public IReadOnlyCollection<string> RegisteredTypes => _factories.Keys.ToList();

        public void Register(string typeKey, Func<JsonElement?, ISetupStep> factory)
        {
            if (string.IsNullOrWhiteSpace(typeKey))
                throw new ArgumentException("Type key must not be empty.", nameof(typeKey));
            _factories[typeKey] = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public bool CanCreate(string typeKey)
            => !string.IsNullOrWhiteSpace(typeKey) && _factories.ContainsKey(typeKey);

        public ISetupStep Create(SetupStepDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            if (string.IsNullOrWhiteSpace(definition.Type))
                throw new InvalidOperationException(
                    $"Step '{definition.StepId}' has no Type. Set it to one of: {KnownTypes()}.");

            if (!_factories.TryGetValue(definition.Type, out var factory))
                throw new InvalidOperationException(
                    $"Step '{definition.StepId}' names unknown type '{definition.Type}'. " +
                    $"Known types: {KnownTypes()}.");

            return factory(definition.Options);
        }

        private string KnownTypes()
            => string.Join(", ", _factories.Keys.OrderBy(k => k, StringComparer.Ordinal));

        private void RegisterBuiltIns()
        {
            Register(TypeKeys.DriverProvision, opts =>
                new DriverProvisionStep(Bind<DriverProvisionStepOptions>(opts) ?? new DriverProvisionStepOptions()));

            Register(TypeKeys.ConnectionConfig, opts =>
                new ConnectionConfigStep(Bind<ConnectionConfigStepOptions>(opts) ?? new ConnectionConfigStepOptions()));

            Register(TypeKeys.SchemaSetup, opts =>
                new SchemaSetupStep(Bind<SchemaSetupStepOptions>(opts) ?? new SchemaSetupStepOptions()));

            Register(TypeKeys.DefaultsSetup, opts =>
                new DefaultsSetupStep(Bind<DefaultsSetupStepOptions>(opts) ?? new DefaultsSetupStepOptions()));

            Register(TypeKeys.Seeding, opts =>
            {
                var o = Bind<SeedingStepOptions>(opts) ?? new SeedingStepOptions();
                // From DI, never from JSON.
                o.Registry = _seeders;
                return new SeedingStep(o);
            });

            Register(TypeKeys.DataImport, opts =>
                new DataImportStep(Bind<DataImportStepOptions>(opts) ?? new DataImportStepOptions()));
        }

        private static T Bind<T>(JsonElement? options) where T : class
        {
            if (options == null || options.Value.ValueKind == JsonValueKind.Null
                                || options.Value.ValueKind == JsonValueKind.Undefined)
                return null;

            try
            {
                return options.Value.Deserialize<T>(OptionsRead);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Could not bind options to {typeof(T).Name}: {ex.Message}", ex);
            }
        }
    }
}
