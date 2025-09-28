# ETLScriptManager how-to

Purpose
- Persist, validate, and execute ETL scripts outside of `ETLEditor`.

Load/save/delete
```csharp
var mgr = new ETLScriptManager(dmeEditor);
var scripts = mgr.LoadScripts();

mgr.SaveScript(etl.Script);        // persist current script
mgr.DeleteScript("script-id");    // remove by id/filename
```

Validate
```csharp
var validate = mgr.ValidateScript(etl.Script);
if (validate.Flag != Errors.Ok) { /* handle */ }
```

Execute
```csharp
await mgr.ExecuteScriptAsync(
    etl.Script,
    progress: new Progress<PassedArgs>(p => Console.WriteLine(p.Messege)),
    token: CancellationToken.None,
    customTransformation: r => r // optional per-record transformation
);
```

Notes
- Scripts are serialized to JSON under `ConfigEditor.ExePath/ Scripts`.
- `ExecuteScriptAsync` fetches, optionally transforms, and inserts in batches.
- Prefer `ETLEditor` for full create+copy orchestration; use this for lightweight execution.
