# Forms Interfaces

## File Map (split from monolithic IUnitofWorksManagerInterfaces.cs)

| File | Types | Lines |
|------|-------|-------|
| `IUnitofWorksManager.cs` | Core coordinator interface | ~340 |
| `IValidationAndLov.cs` | `IValidationManager` + event args, `ILOVManager` + event args, `IItemPropertyManager` | ~775 |
| `ITriggerSystem.cs` | `ITriggerManager`, `ITriggerExecutionLog`, `ITriggerDependencyManager`, `TriggerExecutionLogEntry` | ~390 |
| `ICoreHelpers.cs` | `IEventManager`, `IFormsSimulationHelper`, `IPerformanceManager`, `IConfigurationManager`, `ISystemVariablesManager`, `IDirtyStateManager` | ~310 |
| `IDataOperations.cs` | `ISavepointManager`, `ILockManager`, `IQueryBuilderManager`, `IBlockErrorLog`, `IMessageQueueManager`, `IBlockFactory`, `IBlockPropertyManager`, `IPagingManager` | ~340 |
| `ISecurityAndAudit.cs` | `ISecurityManager`, `IFieldMaskProvider`, `IAuditManager`, `IAuditStore` | ~230 |
| `IProviders.cs` | `IAlertProvider`, `ISequenceProvider`, `ITimerManager` + `TimerFiredEventArgs` | ~125 |
| `IMultiForm.cs` | `IFormRegistry`, `IFormMessageBus`, `ISharedBlockManager` | ~135 |

All interfaces share namespace `TheTechIdea.Beep.Editor.UOWManager.Interfaces`.

## Integration Notes
- Interfaces are designed to keep UI framework code separate from data logic.
- Preserve async entry points for save/rollback and synchronization operations.
- Keep validation hooks consistent so forms and APIs can share rule logic.
