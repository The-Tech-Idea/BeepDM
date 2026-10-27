namespace TheTechIdea.Beep.DataBase
{
 /// <summary>Explicit physical batch budget; neither value grants application permissions.</summary>
 public enum ConditionalMutationBatchBudget
 {
  /// <summary>Existing 64-operation and 8192-scalar contract.</summary>
  Standard,
  /// <summary>Complete documents: 1024 operations, 512 cohorts, 65536 scalars and16 MiB encoded request.</summary>
  Document
 }
}
