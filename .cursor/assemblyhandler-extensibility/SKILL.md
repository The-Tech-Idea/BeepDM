# AssemblyHandler Extensibility — Skill

## Purpose
Step-by-step checklist for making **any BeepDM subsystem discoverable as a plugin** via `AssemblyHandler`. Follow this skill whenever you want a new interface (like `IDefaultValueResolver`, `IFileFormatReader`, etc.) to be auto-discovered from third-party assemblies.

---

## The Pattern — 8 Artefacts

Every extensible subsystem needs **8 artefacts** in a fixed layout:

| # | Artefact | Project | Location pattern |
|---|----------|---------|-----------------|
| 1 | `[SubsystemAttribute]` | `DataManagementModelsStandard` | `Editor/<Subsystem>/Attributes/<Name>Attribute.cs` |
| 2 | `IsXxx` + metadata on `AssemblyClassDefinition` | `DataManagementModelsStandard` | `ConfigUtil/AssemblyClassDefinition.cs` |
| 3 | `XxxClasses` on `IConfigEditor` | `DataManagementModelsStandard` | `ConfigUtil/IConfigEditor.cs` |
| 4 | `XxxClasses` on `ConfigEditor` (×3 locations) | `DataManagementEngineStandard` | `ConfigUtil/ConfigEditor.cs` |
| 5 | Scanning hook in `ProcessTypeInfo` | `DataManagementEngineStandard` | `AssemblyHandler/AssemblyHandler.Scanning.cs` |
| 6 | Attribute-reading hook in `GetAssemblyClassDefinition` | `DataManagementEngineStandard` | `AssemblyHandler/AssemblyHandler.Helpers.cs` |
| 7 | `XxxDescriptor` | `DataManagementEngineStandard` | `Editor/<Subsystem>/Registry/<Name>Descriptor.cs` |
| 8 | `XxxRegistry` with `Discover()` | `DataManagementEngineStandard` | `Editor/<Subsystem>/Registry/<Name>Registry.cs` |

---

## Step-by-Step

### 1. Create `[XxxAttribute]` (Models project)

```csharp
// DataManagementModelsStandard/Editor/<Subsystem>/Attributes/<Name>Attribute.cs
namespace TheTechIdea.Beep.Editor.<Subsystem>.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class <Name>Attribute : Attribute
    {
        public string Name     { get; }        // required — unique key
        public string DisplayName { get; }     // required — UI label
        public string Description { get; set; } = string.Empty;
        public string Author     { get; set; } = "The-Tech-Idea";
        public string Version    { get; set; } = "1.0.0";
        public string IconPath   { get; set; } = string.Empty;
        // Add any domain-specific required ctor params here

        public <Name>Attribute(string name, string displayName)
        {
            Name        = name        ?? throw new ArgumentNullException(nameof(name));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        }
    }
}
```

**Real examples**: `FileReaderAttribute` (Models/FileManager/Attributes), `DefaultResolverAttribute` (Models/Editor/Defaults/Attributes).

---

### 2. Add `IsXxx` + metadata to `AssemblyClassDefinition`

File: `DataManagementModelsStandard/ConfigUtil/AssemblyClassDefinition.cs`

Append **after the last existing plugin-flag block** (currently after `FileReaderExtension`):

```csharp
// ── <Subsystem> plugin properties ────────────────────────────────────

private bool _is<Name> = false;
/// <summary>True when this type implements <c>I<Name></c>
/// and is decorated with <c>[<Name>Attribute]</c>.</summary>
public bool Is<Name>
{
    get { return _is<Name>; }
    set { SetProperty(ref _is<Name>, value); }
}

private string _<name>Key = string.Empty;
/// <summary>Unique key from <c>[<Name>Attribute(name, …)]</c>.</summary>
public string <Name>Key
{
    get { return _<name>Key; }
    set { SetProperty(ref _<name>Key, value); }
}
```

Use `SetProperty(ref field, value)` — same pattern as `IsFileReader`, `IsRule`, `IsPipelinePlugin`.

---

### 3. Add `XxxClasses` to `IConfigEditor`

File: `DataManagementModelsStandard/ConfigUtil/IConfigEditor.cs`

Add **after `FileReaderClasses`**:

```csharp
/// <summary>Third-party <c>I<Name></c> implementations discovered by AssemblyHandler.</summary>
List<AssemblyClassDefinition> <Name>Classes { get; set; }
```

---

### 4. Add `XxxClasses` to `ConfigEditor` (3 locations)

File: `DataManagementEngineStandard/ConfigUtil/ConfigEditor.cs`

**Location A — `InitializeProperties()`** (around line 89):
```csharp
<Name>Classes = new List<AssemblyClassDefinition>();
```

**Location B — Property declaration block** (around line 212, after `FileReaderClasses`):
```csharp
public List<AssemblyClassDefinition> <Name>Classes { get; set; } = new List<AssemblyClassDefinition>();
```

**Location C — `Dispose()` clear block** (around line 667):
```csharp
<Name>Classes?.Clear();
```

---

### 5. Add scanning hook to `AssemblyHandler.Scanning.cs`

File: `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Scanning.cs`

In `ProcessTypeInfo`, **after the `IFileFormatReader` block** (and after the `IDefaultValueResolver` block if already present):

```csharp
// Check for I<Name> interface (third-party <subsystem> plugins)
if (typeInfo.ImplementedInterfaces.Contains(typeof(TheTechIdea.Beep.<Namespace>.I<Name>)))
{
    if (ConfigEditor.<Name>Classes != null)
        ConfigEditor.<Name>Classes.Add(GetAssemblyClassDefinition(typeInfo, "I<Name>"));
}
```

Use the **fully-qualified type name** — no extra `using` is needed in this partial class.

---

### 6. Add attribute-reading hook to `AssemblyHandler.Helpers.cs`

File: `DataManagementEngineStandard/AssemblyHandler/AssemblyHandler.Helpers.cs`

In `GetAssemblyClassDefinition`, **after the `DefaultResolverAttribute` block** (insert in the same chain of attribute checks):

```csharp
// Check for <Name>Attribute (third-party I<Name> plugins)
var <name>Attr = (TheTechIdea.Beep.<Namespace>.Attributes.<Name>Attribute)type.GetCustomAttribute(
    typeof(TheTechIdea.Beep.<Namespace>.Attributes.<Name>Attribute), false);
if (<name>Attr != null)
{
    xcls.Is<Name> = true;
    xcls.<Name>Key = <name>Attr.Name;           // adjust to your key property
    xcls.RootName  = <name>Attr.DisplayName;
    xcls.Imagename = <name>Attr.IconPath;
    if (string.IsNullOrEmpty(xcls.Version)) xcls.Version = <name>Attr.Version;
}
```

---

### 7. Create `XxxDescriptor`

File: `DataManagementEngineStandard/Editor/<Subsystem>/Registry/<Name>Descriptor.cs`

```csharp
namespace TheTechIdea.Beep.Editor.<Subsystem>.Registry
{
    public sealed class <Name>Descriptor
    {
        public <Name>Attribute Attribute      { get; }
        public Type            ImplementationType { get; }

        public <Name>Descriptor(<Name>Attribute attribute, Type implementationType)
        {
            Attribute          = attribute          ?? throw new ArgumentNullException(nameof(attribute));
            ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
        }
    }
}
```

---

### 8. Create `XxxRegistry` with `Discover()`

File: `DataManagementEngineStandard/Editor/<Subsystem>/Registry/<Name>Registry.cs`

```csharp
public class <Name>Registry
{
    private readonly IDMEEditor _editor;
    private readonly Dictionary<string, <Name>Descriptor> _descriptors =
        new Dictionary<string, <Name>Descriptor>(StringComparer.OrdinalIgnoreCase);

    public <Name>Registry(IDMEEditor editor)
        => _editor = editor ?? throw new ArgumentNullException(nameof(editor));

    /// <summary>
    /// Scans ConfigEditor.<Name>Classes, builds descriptors, and auto-registers
    /// each plugin with the appropriate manager (e.g. DefaultsManager.RegisterCustomResolver).
    /// Safe to call multiple times.
    /// </summary>
    public void Discover()
    {
        _editor.ErrorObject.Flag = Errors.Ok;
        try
        {
            var classes = _editor.assemblyHandler?.ConfigEditor?.<Name>Classes;
            if (classes == null) return;

            foreach (var cls in classes)
            {
                if (cls.type == null || !cls.Is<Name>) continue;
                var attr = cls.type.GetCustomAttribute<<Name>Attribute>(inherit: false);
                if (attr == null) continue;

                _descriptors[attr.Name] = new <Name>Descriptor(attr, cls.type);

                // Auto-register with the subsystem manager
                try
                {
                    var instance = (I<Name>)Activator.CreateInstance(cls.type);
                    if (instance != null)
                        <Manager>.Register(_editor, instance);  // adapt to your manager API
                }
                catch (Exception ex)
                {
                    _editor.AddLogMessage(nameof(Discover),
                        $"Could not instantiate '{cls.type.FullName}': {ex.Message}",
                        DateTime.Now, -1, null, Errors.Warning);
                }
            }
        }
        catch (Exception ex)
        {
            _editor.ErrorObject.Flag = Errors.Failed;
            _editor.AddLogMessage(nameof(Discover),
                $"<Name>Registry.Discover failed: {ex.Message}",
                DateTime.Now, -1, null, Errors.Failed);
        }
    }

    public IReadOnlyDictionary<string, <Name>Descriptor> GetDescriptors() => _descriptors;
    public bool TryGet(string name, out <Name>Descriptor d) => _descriptors.TryGetValue(name, out d);

    public I<Name> Create(string name)
    {
        if (!TryGet(name, out var desc)) return null;
        try { return (I<Name>)Activator.CreateInstance(desc.ImplementationType); }
        catch (Exception ex)
        {
            _editor.AddLogMessage(nameof(Create), $"Failed to create '{name}': {ex.Message}",
                DateTime.Now, -1, null, Errors.Warning);
            return null;
        }
    }
}
```

---

## API Notes

### `AddLogMessage` signature
```csharp
_editor.AddLogMessage(string pmethod, string comment, DateTime time, int linenumber, string? source, Errors flag);
```
Always pass `DateTime.Now`, `-1`, `null` for the middle parameters when calling from registry code.

### `AssemblyClassDefinition.type`
The `type` property holds the reflected `System.Type`. It is populated by `AssemblyHandler` after loading. Always null-check before use.

### Plugin-instantiation convention
Resolvers / readers with **no-arg constructors** are created with `Activator.CreateInstance(type)`.  
Plugins that need `IDMEEditor` should accept it as a constructor parameter — use `Activator.CreateInstance(type, _editor)` and catch `MissingMethodException` with fallback to the no-arg ctor.

---

## Consumer: How to write a plugin

```csharp
// 1. Reference the Models NuGet / project
// 2. Implement the interface
// 3. Decorate with the attribute

[DefaultResolver("MyResolver", "My Custom Resolver",
    SupportedTokens = "MYTOKEN,CUSTOM",
    Description = "Resolves values from my custom context",
    Version = "1.2.0")]
public class MyCustomResolver : IDefaultValueResolver
{
    public string ResolverName => "MyResolver";
    public IEnumerable<string> SupportedRuleTypes => new[] { "MYTOKEN", "CUSTOM" };
    public bool CanHandle(string rule) => SupportedRuleTypes.Contains(rule.TrimStart(':').ToUpperInvariant());
    public object ResolveValue(string rule, IPassedArgs parameters) => "my-value";
    public IEnumerable<string> GetExamples() => new[] { ":MYTOKEN", ":CUSTOM" };
}

// 4. Drop the DLL into the plugins folder — AssemblyHandler auto-discovers it
// 5. Call registry.Discover() once after beepService.LoadAssemblies(progress)
```

---

## Existing implementations (reference)

| Subsystem | Attribute | Interface | Registry |
|-----------|-----------|-----------|----------|
| Data Sources (Connectors) | `AddinAttribute` | `IDataSource` | built into `AssemblyHandler` directly |
| Pipeline Plugins | `PipelinePluginAttribute` | `IPipelinePlugin` | `PipelinePluginClasses` list |
| File Format Readers | `FileReaderAttribute` | `IFileFormatReader` | `FileReaderRegistry` |
| Default Value Resolvers | `DefaultResolverAttribute` | `IDefaultValueResolver` | `DefaultResolverRegistry` |
| Rules | `RuleAttribute` | — | `Rules` list |
| Rule Parsers | `RuleParserAttribute` | — | `RuleParserClasses` list |

### Built-in resolvers decorated with `[DefaultResolverAttribute]`

All 9 built-in resolvers in `Editor/Defaults/Resolvers/` carry the attribute:

| Class | ResolverName | Key tokens |
|-------|-------------|------------|
| `ConfigurationResolver` | `"Configuration"` | `CONFIG,APPSETTING,CONNECTIONSTRING` |
| `DataSourceResolver` | `"DataSource"` | `GETENTITY,LOOKUP,QUERY,COUNT,MAX,MIN,SUM,AVG` |
| `DateTimeResolver` | `"DateTime"` | `NOW,TODAY,ADDDAYS,FORMAT,STARTOFMONTH` |
| `EnvironmentResolver` | `"Environment"` | `ENV,ENVIRONMENTVARIABLE,TEMP,SYSTEMPATH` |
| `ExpressionResolver` | `"Expression"` | `IF,CASE,ISNULL,COALESCE,EVAL,EQ,GT,LT` |
| `FormulaResolver` | `"Formula"` | `SEQUENCE,RANDOM,ADD,SUBTRACT,MULTIPLY,DIVIDE` |
| `GuidResolver` | `"Guid"` | `NEWGUID,GUID,UUID` |
| `ObjectPropertyResolver` | `"ObjectProperty"` | `PROPERTY,FIELD,OBJECTVALUE,NESTED` |
| `SystemInfoResolver` | `"SystemInfo"` | `MACHINENAME,OSVERSION,PROCESSORCOUNT` |
| `UserContextResolver` | `"UserContext"` | `USERNAME,USERID,USERDOMAIN,USERROLE` |

> **Decorating built-in classes** with the attribute is required so that `DefaultResolverRegistry.Discover()` can catalog them for tooling/reflection queries, even though they are also hard-registered in `DefaultsManager.EnsureInitialized()`.

---

## Constructor handling in `XxxRegistry`

Built-in resolvers typically require `IDMEEditor`; third-party plugins may use a no-arg ctor.  
Use reflection to pick the right constructor — **do not** use nested `try/catch MissingMethodException`:

```csharp
// In Discover():
var editorCtor = cls.type.GetConstructor(new[] { typeof(IDMEEditor) });
var instance = editorCtor != null
    ? (IDefaultValueResolver)editorCtor.Invoke(new object[] { _editor })
    : (IDefaultValueResolver)Activator.CreateInstance(cls.type);

// In Create():
var editorCtor = desc.ImplementationType.GetConstructor(new[] { typeof(IDMEEditor) });
return editorCtor != null
    ? (IDefaultValueResolver)editorCtor.Invoke(new object[] { _editor })
    : (IDefaultValueResolver)Activator.CreateInstance(desc.ImplementationType);
```

Requires `using System.Reflection;` in the registry file (already imported via the standard registry template).

---

## File locations (real paths)

```
DataManagementModelsStandard/
  ConfigUtil/
    AssemblyClassDefinition.cs       ← add IsXxx + XxxKey props here
    IConfigEditor.cs                 ← add XxxClasses interface prop here
  FileManager/Attributes/
    FileReaderAttribute.cs           ← reference implementation
  Editor/Defaults/Attributes/
    DefaultResolverAttribute.cs      ← second implementation

DataManagementEngineStandard/
  ConfigUtil/
    ConfigEditor.cs                  ← add XxxClasses in 3 places
  AssemblyHandler/
    AssemblyHandler.Scanning.cs      ← add interface check in ProcessTypeInfo
    AssemblyHandler.Helpers.cs       ← add attribute detection in GetAssemblyClassDefinition
  FileManager/Registry/
    FileReaderDescriptor.cs          ← reference descriptor impl
    FileReaderRegistry.cs            ← reference registry impl
  Editor/Defaults/Registry/
    DefaultResolverDescriptor.cs     ← second descriptor impl
    DefaultResolverRegistry.cs       ← second registry impl
```
