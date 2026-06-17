using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TheTechIdea.Beep.DragDrop;

[DataContract]
[Serializable]
public sealed class BeepEntityDragPayload
{
    [DataMember]
    public string EntityName { get; set; } = string.Empty;

    [DataMember]
    public string ConnectionName { get; set; } = string.Empty;

    [DataMember]
    public string? DataSourceCategory { get; set; }

    [DataMember]
    public List<BeepFieldDragItem> Fields { get; set; } = new();

    [DataMember]
    public bool IsMaster { get; set; }

    [DataMember]
    public string? MasterKeyField { get; set; }

    [DataMember]
    public string? PreferredBlockName { get; set; }
}

[DataContract]
[Serializable]
public sealed class BeepFieldDragItem
{
    [DataMember]
    public string FieldName { get; set; } = string.Empty;

    [DataMember]
    public string DataType { get; set; } = string.Empty;

    [DataMember]
    public bool IsPrimaryKey { get; set; }

    [DataMember]
    public bool IsRequired { get; set; }

    [DataMember]
    public int FieldSize { get; set; }
}
