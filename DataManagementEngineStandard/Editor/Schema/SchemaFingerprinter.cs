using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TheTechIdea.Beep.Editor.Schema
{
    public sealed class SchemaFingerprinter : ISchemaFingerprinter
    {
        public string ComputeSchemaHash(DataSyncSchema schema)
        {
            if (schema == null) return Guid.NewGuid().ToString("N");

            try
            {
                var fp = new StringBuilder(256)
                    .Append(schema.SourceDataSourceName      ?? string.Empty).Append('|')
                    .Append(schema.DestinationDataSourceName ?? string.Empty).Append('|')
                    .Append(schema.SourceEntityName          ?? string.Empty).Append('|')
                    .Append(schema.DestinationEntityName     ?? string.Empty).Append('|')
                    .Append(schema.SyncDirection             ?? string.Empty).Append('|')
                    .Append(schema.SyncType                   ?? string.Empty);

                if (schema.MappedFields != null && schema.MappedFields.Count > 0)
                {
                    var fields = schema.MappedFields
                        .Where(f => f != null)
                        .Select(f => $"{(f.SourceField ?? string.Empty)}:{(f.DestinationField ?? string.Empty)}")
                        .OrderBy(s => s, StringComparer.Ordinal);

                    fp.Append('|').Append(string.Join(",", fields));
                }

                var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(fp.ToString()));
                return Convert.ToHexString(bytes).ToLowerInvariant();
            }
            catch
            {
                return Guid.NewGuid().ToString("N");
            }
        }
    }
}
