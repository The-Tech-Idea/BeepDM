namespace TheTechIdea.Beep.Editor.Schema
{
    /// <summary>
    /// Stable, comparable fingerprints for BeepDM schema artifacts.
    /// </summary>
    public interface ISchemaFingerprinter
    {
        string ComputeSchemaHash(DataSyncSchema schema);
    }
}
