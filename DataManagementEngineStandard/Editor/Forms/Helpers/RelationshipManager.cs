// This file was removed in audit pass 3 (2026-06) along with the
// IRelationshipManager interface (IUnitofWorksManagerInterfaces.cs:376).
// The active master/detail orchestration lives in:
//   - FormsManager.Relationships.cs  (CreateMasterDetailRelation, etc.)
//   - FormsManager.Helpers.cs        (SynchronizeDetailHierarchyAsync)
//   - Helpers/MasterDetailKeyResolver.cs (key resolution)
// External hosts that depended on the obsolete RelationshipManager class
// or IRelationshipManager interface must migrate to the FormsManager
// master/detail methods, which is what the [Obsolete] attribute has been
// recommending for some time.
