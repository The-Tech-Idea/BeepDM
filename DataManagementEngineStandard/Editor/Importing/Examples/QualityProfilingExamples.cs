// Example — Quality + ErrorStore + History + Watermark
//
// Documents the public surface of the quality evaluator, error-store
// round-trip, and watermark store.  Uses only types that compile against
// the current engine surface.

using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Importing.ErrorStore;
using TheTechIdea.Beep.Editor.Importing.Quality;
using TheTechIdea.Beep.Editor.Importing.Sync;

namespace TheTechIdea.Beep.Editor.Importing.Examples
{
    /// <summary>
    /// Phase 4 — quality rules, error store, and watermark store.
    /// BUILD-compatible: calls only the currently available engine surface.
    /// </summary>
    public static class QualityProfilingExamples
    {
        /// <summary>
        /// Build a list of built-in quality rules using the constructor-only API.
        /// All rules take a field name + failure action via constructor params.
        /// </summary>
        public static List<IDataQualityRule> BuildRules()
        {
            var rules = new List<IDataQualityRule>
            {
                new NotNullRule ("Email",    DataQualityAction.Block),
                new UniqueRule  ("Email",    DataQualityAction.Quarantine),
                new RegexRule   ("Email",    @"^[^\s@]+@[^\s@]+\.[^\s@]+$",
                                onFailure:   DataQualityAction.Warn),
                new RangeRule   ("Age",      0, 150,
                                onFailure:   DataQualityAction.Block)
            };

            Console.WriteLine($"Registered {rules.Count} quality rule(s).");
            return rules;
        }

        /// <summary>
        /// Demonstrate the JSON-file error store.
        /// <c>JsonFileImportErrorStore</c> persists errors at
        ///   <c>{configRoot}/Importing/Errors/&lt;key&gt;.errors.jsonl</c>.
        /// The <c>DataSourceImportErrorStore</c> follows the same contract for DB-backed
        /// environments and is interchangeable at the <c>IImportErrorStore</c> level.
        /// </summary>
        public static void RunErrorStoreDemo()
        {
            var store = new JsonFileImportErrorStore();
            Console.WriteLine("JsonFileImportErrorStore constructed — errors land under the editor config root.");
        }

        /// <summary>
        /// Demonstrate the in-memory watermark store.
        /// <c>FileWatermarkStore</c> follows the same <c>IWatermarkStore</c> contract.
        /// </summary>
        public static void RunWatermarkStoreDemo()
        {
            var watermark = new InMemoryWatermarkStore();
            Console.WriteLine("InMemoryWatermarkStore constructed.");
        }
    }
}
