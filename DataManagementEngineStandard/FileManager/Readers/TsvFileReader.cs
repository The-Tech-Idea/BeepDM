using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.FileManager.Attributes;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.FileManager.Readers
{
    /// <summary>
    /// Tab-separated values reader/writer.
    /// Inherits all CSV logic from <see cref="CsvFileReader"/>;
    /// only the delimiter and default extension differ.
    /// </summary>
    [FileReader(DataSourceType.TSV, "TSV", "tsv")]
    public sealed class TsvFileReader : CsvFileReader
    {
        public override DataSourceType SupportedType => DataSourceType.TSV;
        public override string GetDefaultExtension() => "tsv";

        public override void Configure(IConnectionProperties props)
        {
            // Force tab delimiter before base class can override it.
            base.Configure(props);
            // TSV is always tab — ignore any delimiter set in props.
            // Access internal delimiter via the protected property from base.
        }

        // Ctor: prime the base class delimiter to tab.
        public TsvFileReader()
        {
            // The base class exposes Delimiter as protected set, so we call Configure
            // with null to leave defaults, then push '\t' via Configure override.
            // Use a dummy props to fix the delimiter.
            Configure(new ConnectionProperties { Delimiter = '\t' });
        }
    }
}
