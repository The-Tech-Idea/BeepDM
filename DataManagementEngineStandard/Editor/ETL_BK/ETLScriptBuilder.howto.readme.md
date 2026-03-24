# ETLScriptBuilder how-to

Purpose
- Build `ETLScriptDet` lists (CreateEntity, CopyData) from entity metadata to keep `ETLEditor` lean.

Create entity scripts
```csharp
var builder = new ETLScriptBuilder(dmeEditor);
var createScripts = builder.BuildCreateEntityScripts(
    src: sourceDs,
    dest: destDs,
    entities: sourceEntities,
    copyData: true,
    progress: new Progress<PassedArgs>(p => Console.WriteLine(p.Messege)),
    token: CancellationToken.None);
```

Copy data scripts
```csharp
var copyScripts = builder.BuildCopyDataScripts(
    src: sourceDs,
    dest: destDs,
    entities: sourceEntities,
    progress: progress,
    token: token);
```

Notes
- `SourceEntity` is embedded for downstream processing when available.
- `Mapping` is initialized per script for later customization.
- Use `token.ThrowIfCancellationRequested()` integration for cooperative cancellation.
