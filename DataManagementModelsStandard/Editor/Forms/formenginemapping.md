# Forms Engine → IDE Type Mapping

**274 types total** (72 Engine + 202 Models).

## IDE Usage Map — What to Use Instead of WinForms Types

### Scanner/Form Discovery

| IDE Old Type (WinFormsScanner.cs) | Engine Replacement | Location |
|-----------------------------------|-------------------|----------|
| `FormInfo` | `ScannedFormInfo` | `Models\ScannedFormInfo.cs` |
| `BlockInfo` | `ScannedBlockInfo` | `Models\ScannedFormInfo.cs` |
| `ItemInfo` (scanner's) | `ScannedItemInfo` | `Models\ScannedFormInfo.cs` |
| `TriggerInfo` | `ScannedTriggerInfo` | `Models\ScannedFormInfo.cs` |
| `LOVInfo` | `ScannedLovInfo` | `Models\ScannedFormInfo.cs` |
| `ValidationInfo` | `ScannedValidationInfo` | `Models\ScannedFormInfo.cs` |
| `FormsHostInfo` | `ScannedHostInfo` | `Models\ScannedFormInfo.cs` |
| `BlockRuntimeKind` | `ScannedBlockRuntimeKind` | `Models\ScannedFormInfo.cs` |
| `WinFormsScanner` | `IFormScanner` | `Hosts\IFormScanner.cs` |

### Block/Item/Form State

| IDE Need | Engine Type | Location |
|----------|-------------|----------|
| Block metadata | `DataBlockInfo` | `Models\DataBlockInfo.cs` |
| Block runtime status | `BlockStatus` | `Models\BlockStatus.cs` |
| Item definition | `ItemInfo` | `Models\ItemInfo.cs` |
| Form state | `FormStateSnapshot` | `Models\FormStateSnapshot.cs` |
| Form host operations | `IBeepFormsHost` | `Hosts\IBeepFormsHost.cs` |

### Triggers/LOVs/Validations

| IDE Need | Engine Type | Location |
|----------|-------------|----------|
| Trigger definition | `TriggerDefinition` | `Models\TriggerDefinition.cs` |
| Trigger types | `TriggerType` (enum) | `Models\TriggerEnums.cs` |
| LOV definition | `LOVDefinition` | `Models\LOVDefinition.cs` |
| LOV result | `LOVResult` | `Models\LOVResult.cs` |
| Validation rule | `ValidationRule` | `Models\ValidationRule.cs` |
| Trigger manager | `ITriggerManager` | `Interfaces\ITriggerSystem.cs` |
| LOV manager | `ILOVManager` | `Interfaces\IValidationAndLov.cs` |
| Validation manager | `IValidationManager` | `Interfaces\IValidationAndLov.cs` |
| Item properties | `IItemPropertyManager` | `Interfaces\IValidationAndLov.cs` |

### Navigation/CRUD

| IDE Need | Engine Type | Location |
|----------|-------------|----------|
| Navigate records | `IUnitofWorksManager` (FirstRecordAsync, etc.) | `Interfaces\IUnitofWorksManager.cs` |
| CRUD operations | `IUnitofWorksManager` (InsertRecordAsync, DeleteCurrentRecordAsync) | `Interfaces\IUnitofWorksManager.cs` |
| Query mode | `IUnitofWorksManager` (EnterQueryAsync, ExecuteQueryAsync) | `Interfaces\IUnitofWorksManager.cs` |
| Block view | `IBlockView` | `Hosts\IBlockView.cs` |
| Navigation bar | `IBlockNavigationBar` | `Hosts\IBlockNavigationBar.cs` |

### Connection/Data

| IDE Need | Engine Type | Location |
|----------|-------------|----------|
| Connection catalog | `IConnectionCatalogRepository` | `ConfigUtil\IConnectionCatalogRepository.cs` |
| Config editor | `IConfigEditor` | `ConfigUtil\IConfigEditor.cs` |
| Editor root | `IDMEEditor` | `Editor\IDMEEditor.cs` |
| Form registry | `IFormRegistry` | `Interfaces\IMultiForm.cs` |
| Active forms | `IUnitofWorksManager` (StandaloneBlocks, GetBlockStatus) | `Interfaces\IUnitofWorksManager.cs` |

### Multi-Form

| IDE Need | Engine Type | Location |
|----------|-------------|----------|
| Call form | `IUnitofWorksManager.CallFormAsync` | `Interfaces\IUnitofWorksManager.cs` |
| Open form | `IUnitofWorksManager.OpenFormModelessAsync` | `Interfaces\IUnitofWorksManager.cs` |
| Global variables | `IUnitofWorksManager.SetGlobalVariable / GetGlobalVariable` | `Interfaces\IUnitofWorksManager.cs` |
| Inter-form messages | `IUnitofWorksManager.PostMessage / BroadcastMessage` | `Interfaces\IUnitofWorksManager.cs` |

### Misc

| IDE Need | Engine Type | Location |
|----------|-------------|----------|
| Record groups | `RecordGroup` | `Models\RecordGroup.cs` |
| Parameter lists | `ParameterList` | `Models\ParameterList.cs` |
| Client info | `ClientInfo` | `Models\ClientInfo.cs` |
| Savepoints | `SavepointInfo` | `Models\SavepointInfo.cs` |
| Security | `SecurityContext`, `BlockSecurity` | `Models\SecurityModels.cs` |
| Audit | `AuditEntry`, `AuditConfiguration` | `Models\AuditModels.cs` |
| Platform detection | `FormHostPlatform` (enum) | `Models\FormHostPlatform.cs` |
| Field presenter | `IFieldPresenter` | `Hosts\IFieldPresenter.cs` |
