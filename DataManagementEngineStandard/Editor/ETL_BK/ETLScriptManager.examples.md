# ETLScriptManager examples

Example 1: Save and execute a script
```csharp
var mgr = new ETLScriptManager(dmeEditor);

// Assuming etl.Script was prepared earlier
await mgr.ExecuteScriptAsync(
    etl.Script,
    new Progress<PassedArgs>(p => Console.WriteLine(p.Messege)),
    CancellationToken.None,
    customTransformation: r => r);

mgr.SaveScript(etl.Script); // Serialize to JSON
```

Example 2: Load all scripts and execute first one
```csharp
var scripts = mgr.LoadScripts();
if (scripts.Any())
{
    await mgr.ExecuteScriptAsync(scripts[0], new Progress<PassedArgs>(p => Console.WriteLine(p.Messege)), CancellationToken.None);
}
```
