# ETLScriptBuilder examples

Example 1: Create + Copy scripts from metadata
```csharp
var builder = new ETLScriptBuilder(dmeEditor);
var entities = srcDs.Entities.Select(e => srcDs.GetEntityStructure(e.EntityName, true)).ToList();

var createScripts = builder.BuildCreateEntityScripts(srcDs, destDs, entities, copyData: true,
    progress: new Progress<PassedArgs>(p => Console.WriteLine(p.Messege)),
    token: CancellationToken.None);

var copyScripts = builder.BuildCopyDataScripts(srcDs, destDs, entities,
    progress: new Progress<PassedArgs>(p => Console.WriteLine(p.Messege)),
    token: CancellationToken.None);

etl.Script.ScriptDTL.AddRange(createScripts);
etl.Script.ScriptDTL.AddRange(copyScripts);
```

Example 2: Only specific entities
```csharp
var wanted = new[]{"Customers","Orders"};
var ents = srcDs.Entities
    .Where(e => wanted.Contains(e.EntityName))
    .Select(e => srcDs.GetEntityStructure(e.EntityName, true));

var scripts = builder.BuildCreateEntityScripts(srcDs, destDs, ents, copyData: false,
    new Progress<PassedArgs>(p => Console.WriteLine(p.Messege)), CancellationToken.None);
```
