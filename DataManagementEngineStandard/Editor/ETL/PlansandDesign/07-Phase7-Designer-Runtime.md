# Phase 7 — Visual Designer & Runtime Monitor

**Version:** 1.0  
**Date:** 2026-03-13  
**Status:** Design  
**Depends on:** All previous phases

> ⚠️ **Separate Project** — Phase 7 is implemented in a standalone UI project/solution.  
> It does **not** live inside `BeepDM` or `DataManagementEngineStandard`.  
> It references `TheTechIdea.Beep.Pipelines` (Phases 1–6) only as NuGet packages.  
> This keeps the engine free of any UI dependency.

---

## 1. Separate Solution

```
Solution:  TheTechIdea.Beep.Pipelines.Designer.sln
Projects:
  TheTechIdea.Beep.Pipelines.Designer          ← WinForms controls library
  TheTechIdea.Beep.Pipelines.Designer.Host     ← standalone designer app
  TheTechIdea.Beep.Pipelines.Designer.Tests    ← UI unit & interaction tests
```

NuGet references (no project references back into BeepDM):
- `TheTechIdea.Beep.DataManagementEngine`
- `TheTechIdea.Beep.Pipelines` (output of Phases 1–6)

---

## 2. Objective

Provide a WinForms-based visual pipeline and workflow designer plus a live runtime monitor that rivals tools like Azure Data Factory's graph canvas or Talend Studio. The designer must:

1. **Zero-code pipeline creation** — drag, drop, connect, configure
2. **Live run monitor** — watch records flow, step by step, in real time
3. **Workflow canvas** — visual DAG for multi-branch workflow design
4. **Schedule calendar** — timeline view of next scheduled runs
5. **Alert dashboard** — at-a-glance health for all pipelines

All controls are standalone `UserControl` classes, embeddable in any WinForms host.

---

## 3. Project Layout & Domain Folders

Every folder maps 1:1 to its sub-namespace. Partial classes split large controls by responsibility.

```
TheTechIdea.Beep.Pipelines.Designer/
│
├── Canvas/                                      ← namespace: .Canvas
│   ├── CanvasNode.cs
│   ├── CanvasEdge.cs
│   ├── CanvasLayout.cs                          ← model + AutoLayout
│   ├── CanvasLayout.AutoLayout.cs               ← partial: Sugiyama layout algorithm
│   └── DesignerState.cs                         ← undo/redo command stack
│
├── Rendering/                                   ← namespace: .Rendering
│   ├── CanvasRenderer.cs
│   ├── NodeRenderer.cs
│   ├── EdgeRenderer.cs
│   └── GridRenderer.cs
│
├── Interaction/                                 ← namespace: .Interaction
│   ├── SelectionManager.cs
│   ├── DragDropManager.cs
│   ├── ConnectionManager.cs
│   └── ContextMenuManager.cs
│
├── Controls/                                    ← namespace: .Controls
│   ├── Pipeline/
│   │   ├── PipelineDesignerControl.cs           ← partial: layout + public API
│   │   ├── PipelineDesignerControl.Mouse.cs     ← partial: mouse event handlers
│   │   ├── PipelineDesignerControl.Keyboard.cs  ← partial: keyboard shortcuts
│   │   └── PipelineDesignerControl.Paint.cs     ← partial: OnPaint + rendering
│   ├── Workflow/
│   │   ├── WorkflowDesignerControl.cs
│   │   ├── WorkflowDesignerControl.Mouse.cs
│   │   └── WorkflowDesignerControl.Paint.cs
│   ├── Palette/
│   │   └── PluginPaletteControl.cs
│   ├── Properties/
│   │   ├── PropertyPanelControl.cs
│   │   ├── PropertyPanelControl.AutoGen.cs      ← partial: auto-generates fields
│   │   └── FieldMappingGrid.cs
│   ├── Monitor/
│   │   ├── PipelineRunMonitorControl.cs
│   │   └── PipelineRunMonitorControl.Live.cs    ← partial: timer + live update
│   ├── Schedule/
│   │   └── ScheduleCalendarControl.cs
│   ├── Alerts/
│   │   └── AlertDashboardControl.cs
│   └── Lineage/
│       └── LineageViewerControl.cs
│
├── Parameters/                                  ← namespace: .Parameters
│   ├── ParameterDefinition.cs
│   └── ParameterEditorFactory.cs               ← creates the right Control by ParamType
│
├── Theme/                                       ← namespace: .Theme
│   └── RenderTheme.cs
│
└── Forms/                                       ← namespace: .Forms
    ├── PipelineDesignerForm.cs
    ├── WorkflowDesignerForm.cs
    └── RunMonitorForm.cs
```

---

## 4. Canvas Model

### 4.1 CanvasNode

```csharp
public class CanvasNode
{
    public string   Id           { get; set; } = Guid.NewGuid().ToString();
    public string   StepId       { get; set; } = string.Empty;   // maps to PipelineStepDef.Id
    public string   PluginId     { get; set; } = string.Empty;
    public string   DisplayName  { get; set; } = string.Empty;
    public NodeKind Kind         { get; set; }
    public PointF   Position     { get; set; }    // canvas coordinates
    public SizeF    Size         { get; set; } = new SizeF(160, 60);
    public bool     IsSelected   { get; set; }
    public NodeState RuntimeState { get; set; } = NodeState.Idle;

    // Port positions (computed from Size, not serialized)
    public PointF InputPort  => new PointF(Position.X, Position.Y + Size.Height / 2);
    public PointF OutputPort => new PointF(Position.X + Size.Width, Position.Y + Size.Height / 2);
}

public enum NodeKind { Source, Transformer, Validator, Sink, Notifier }

public enum NodeState
{
    Idle,       // no run active
    Waiting,    // scheduled but blocked
    Running,    // currently executing — animated pulse
    Success,    // last run succeeded — green
    Failed,     // last run failed — red
    Skipped     // bypassed by conditional edge
}
```

### 4.2 CanvasEdge

```csharp
public class CanvasEdge
{
    public string   Id              { get; set; } = Guid.NewGuid().ToString();
    public string   ConnectionId    { get; set; } = string.Empty; // maps to StepConnection.Id
    public string   FromNodeId      { get; set; } = string.Empty;
    public string   ToNodeId        { get; set; } = string.Empty;
    public string?  ConditionLabel  { get; set; }   // short label for conditional edge
    public bool     IsSelected      { get; set; }
    public EdgeStyle Style          { get; set; } = EdgeStyle.Bezier;
}

public enum EdgeStyle { Bezier, Elbow, Straight }
```

### 4.3 CanvasLayout (serializable to VisualLayoutJson)

```csharp
public class CanvasLayout
{
    public List<CanvasNode> Nodes { get; set; } = new();
    public List<CanvasEdge> Edges { get; set; } = new();
    public float Zoom             { get; set; } = 1.0f;
    public PointF Offset          { get; set; } = PointF.Empty;

    public string ToJson()
        => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

    public static CanvasLayout FromJson(string json)
        => JsonSerializer.Deserialize<CanvasLayout>(json) ?? new();

    /// <summary>Auto-layout nodes as left-to-right flow (Sugiyama simplified).</summary>
    public void AutoLayout(int horizontalGap = 220, int verticalGap = 100)
    {
        // Layer assignment → x position
        // Nodes within a layer → y position spread
        // Called when loading a pipeline that has no saved layout
    }
}
```

---

## 5. Canvas Renderer (GDI+)

Drawing is done on a `Panel` with `DoubleBuffered = true`. All rendering goes through:

```csharp
public class CanvasRenderer
{
    public void Render(Graphics g, CanvasLayout layout, RenderTheme theme)
    {
        // 1. Scale/translate by layout.Zoom and layout.Offset
        g.ScaleTransform(layout.Zoom, layout.Zoom);
        g.TranslateTransform(layout.Offset.X, layout.Offset.Y);

        // 2. Draw snap grid
        _gridRenderer.Draw(g, layout, theme);

        // 3. Draw edges (behind nodes)
        foreach (var edge in layout.Edges)
            _edgeRenderer.Draw(g, edge, layout, theme);

        // 4. Draw nodes (on top)
        foreach (var node in layout.Nodes)
            _nodeRenderer.Draw(g, node, theme);
    }
}
```

### NodeRenderer

Each node draws as a rounded rectangle with:
- **Header stripe** colour-coded by `NodeKind` (blue=Source, teal=Transform, purple=Validator, green=Sink)
- **Plugin icon** (emoji character from PluginPalette registry)
- **Display name** (truncated with ellipsis if too long)
- **Runtime badge** — spinning ring when `NodeState.Running`, coloured dot otherwise
- **Input/Output ports** — small circles on left/right edges

### EdgeRenderer

Bezier curves with cubic control points:
```csharp
PointF c1 = new PointF(from.X + (to.X - from.X) / 2, from.Y);
PointF c2 = new PointF(from.X + (to.X - from.X) / 2, to.Y);
g.DrawBezier(edgePen, from, c1, c2, to);
```

Dashed line for conditional edges; animated `DashOffset` increment during live run.

---

## 6. PipelineDesignerControl

```csharp
public class PipelineDesignerControl : UserControl
{
    // ── Events ───────────────────────────────────────────────────────────
    public event EventHandler<CanvasNode>? NodeSelected;
    public event EventHandler<CanvasNode>? NodeDoubleClicked;
    public event EventHandler? DefinitionChanged;

    // ── Public API ───────────────────────────────────────────────────────
    public void LoadDefinition(PipelineDefinition def);
    public PipelineDefinition GetDefinition();
    public void SetRunState(PipelineRunLog runLog);   // updates NodeState colours
    public void AddNode(string pluginId, PointF position);
    public void DeleteSelectedNodes();
    public void Undo();
    public void Redo();
    public void ZoomIn();
    public void ZoomOut();
    public void ZoomToFit();
    public void AutoLayout();
    public void ExportToPng(string filePath);

    // ── Toolbar helper (call from host form) ─────────────────────────────
    public ToolStrip CreateToolStrip() => /* Run, Stop, Undo, Redo, Zoom, AutoLayout */ ;
}
```

### Keyboard Shortcuts

| Key | Action |
|-----|--------|
| Delete | Delete selected node(s) / edge |
| Ctrl+Z | Undo |
| Ctrl+Y | Redo |
| Ctrl+A | Select all |
| Ctrl+= | Zoom in |
| Ctrl+- | Zoom out |
| Ctrl+0 | Zoom to fit |
| F5 | Run pipeline |
| Escape | Cancel drag / deselect |

---

## 7. PluginPaletteControl

```csharp
public class PluginPaletteControl : UserControl
{
    public event EventHandler<string>? PluginDropRequested;   // pluginId

    public void Refresh(PipelinePluginRegistry registry)
    {
        // Group plugins by PipelinePluginType
        // Render as expandable TreeView:
        //   Sources
        //     📥 CSV Source
        //     🗄️ Database Source
        //     📂 File Source
        //   Transformers
        //     ✂️ Field Map
        //     🔣 Expression
        //     🔀 Type Cast
        //     🔇 Filter
        //     🔡 De-Duplicate
        //     🔍 Lookup
        //     📊 Aggregate
        //     🌿 Split
        //     📝 Script
        //   Validators
        //   Sinks
        //   Notifiers
        //   Schedulers
    }

    // Drag initiation on TreeView.ItemDrag
    // On drop over PipelineDesignerControl, fire PluginDropRequested
}
```

---

## 8. PropertyPanelControl

When the user selects a node, the property panel must auto-generate a config form from the plugin's parameter definitions.

### ParameterDefinition

```csharp
public class ParameterDefinition
{
    public string   Name           { get; set; } = string.Empty;
    public string   DisplayName    { get; set; } = string.Empty;
    public string   Description    { get; set; } = string.Empty;
    public ParamType Type          { get; set; }
    public bool     IsRequired     { get; set; }
    public object?  DefaultValue   { get; set; }
    public string?  ValidationRegex { get; set; }
    public List<string>? Options   { get; set; }   // for Enum type
    public int? MinValue           { get; set; }
    public int? MaxValue           { get; set; }
    public bool IsSensitive        { get; set; }   // render as password box
}

public enum ParamType { String, Integer, Float, Boolean, Enum, ConnectionName, EntityName, FieldMapping, CronExpression, FilePath, Expression, Json }
```

All pipeline plugins implement:
```csharp
public interface IPipelinePlugin
{
    IReadOnlyList<ParameterDefinition> GetParameterDefinitions();
    void ApplyConfig(Dictionary<string, object> config);
    IReadOnlyDictionary<string, string> ValidateConfig(Dictionary<string, object> config);
}
```

### Auto-Generation Rules

| ParamType | WinForms Control |
|-----------|-----------------|
| String | TextBox |
| Integer | NumericUpDown |
| Float | TextBox (validated) |
| Boolean | CheckBox |
| Enum | ComboBox (from Options) |
| ConnectionName | ComboBox (from `DMEEditor.GetDataSources()`) |
| EntityName | ComboBox (from selected data source) |
| FieldMapping | FieldMappingGrid (custom) |
| CronExpression | TextBox + inline cron validator + preview |
| FilePath | TextBox + Browse button |
| Expression | Multiline TextBox + Validate button |
| Json | Multiline TextBox + Format button |
| (IsSensitive) | MaskedTextBox with show/hide toggle |

---

## 9. FieldMappingGrid Control

A central reusable control for the FieldMapTransformer and DataSinkPlugin:

```
┌─────────────────────────────────────────────────────────────────┐
│ Source Field          │ Target Field         │ Transform (opt.)  │
├─────────────────────────────────────────────────────────────────┤
│ [CustomerID    ▼]     │ [CustId         ▼]   │ [           ]     │
│ [FirstName     ▼]     │ [GivenName      ▼]   │ [UPPER({v}) ]     │
│ [BirthDate     ▼]     │ [DateOfBirth    ▼]   │ [           ]     │
│ [+]                   │                      │                   │
└─────────────────────────────────────────────────────────────────┘
```

- Source / target field dropdowns populated from `PipelineSchema`
- Inline expression column for simple per-field transforms
- Drag to reorder rows
- Auto-map button: match by exact name, then by fuzzy match (Levenshtein distance ≤ 2)

---

## 10. WorkflowDesignerControl

Same rendering engine as `PipelineDesignerControl`, but working with `WorkFlowDefinition`:

```csharp
public class WorkflowDesignerControl : UserControl
{
    public event EventHandler<WorkFlowStepDef>? StepSelected;
    public event EventHandler? DefinitionChanged;

    public void LoadDefinition(WorkFlowDefinition def);
    public WorkFlowDefinition GetDefinition();
    public void SetRunState(WorkFlowRunContext ctx);   // updates step colours

    // Node kinds for workflow: StartNode, StepNode, ConditionDiamond, EndNode, SubWorkflowNode
}
```

### Condition Node (Diamond)

When a step has `NextSteps.Count > 1` (conditional fan-out), it renders as a diamond shape with outgoing labelled edges. Each label shows the condition expression truncated to 30 characters.

---

## 11. PipelineRunMonitorControl

Live real-time view of a running pipeline:

```
┌─────────────────────────────────────────────────────────────────┐
│ Pipeline: Nightly Customer Sync   Status: ● RUNNING   [Cancel]  │
│ Run ID: 7f3c...  Started: 14:02:11   Elapsed: 00:01:43          │
├─────────────────────────────────────────────────────────────────┤
│  Step               Status    Records In  Records Out  Rejected  │
│  ─────────────────────────────────────────────────────────────  │
│  ● DB Source        ✓ Done    100,000         —           —      │
│  ◉ Expression Xfm  ↻ Running   73,420      73,420          0    │
│  ○ Null Validator   Waiting      —            —             —    │
│  ○ DB Sink          Waiting      —            —             —    │
├─────────────────────────────────────────────────────────────────┤
│ Throughput: 712 rows/sec   DQ Pass: 99.8%   Errors: 0           │
├─────────────────────────────────────────────────────────────────┤
│ ▼ Error Log (last 100)                                          │
│  14:02:47  WARN  Row 15234: Null value in 'Email' (NotNull)      │
│  14:02:51  WARN  Row 18990: Null value in 'Email' (NotNull)      │
└─────────────────────────────────────────────────────────────────┘
```

### Update Mechanism

`PipelineRunMonitorControl` subscribes to `PipelineEngine.RunProgressEvent` (a new event raised per step completion) and uses `Control.BeginInvoke` to marshal updates to the UI thread, refreshing at 500ms intervals.

```csharp
public class PipelineRunMonitorControl : UserControl
{
    public void AttachRun(string runId, PipelineEngine engine);
    public void Detach();

    // Internal timer fires every 500ms:
    private void RefreshFromLiveMetrics()
    {
        var live = _observabilityStore.GetLiveMetrics(_runId);
        // Update grid rows, throughput label, error log
    }
}
```

---

## 12. ScheduleCalendarControl

A 7-day horizontal timeline showing:
- Past runs (coloured by success/failure)
- Scheduled future runs (light blue blocks)
- Hover tooltip shows: run ID, duration, records

```
Mon 10       Tue 11       Wed 12       Thu 13 (today)   Fri 14
 ■ Cust Sync  ■ Cust Sync  ■ Cust Sync  ■ Cust Sync     □ Cust Sync
 ■ Inv Sync   ■ Inv Sync                ■ Inv Sync       □ Inv Sync
             ✗ DQ Check                ■ DQ Check       □ DQ Check
```

Legend: ■ black = success, ✗ red = failure, □ = scheduled future

Click a block opens the run log detail panel.

---

## 13. AlertDashboardControl

```
┌── Pipeline Health ─────────────────────────────────────────────┐
│ [All Pipelines ▼]  [Last 24h ▼]     ● 2 Critical  ⚠ 5 Warnings │
├─────────────────────────────────────────────────────────────────┤
│  ● 14:55  CRITICAL  Nightly Sync failed — connection timeout     │
│           Pipeline: Customer ETL  Run: 9a1c...   [Ack] [View]   │
│                                                                  │
│  ⚠ 14:12  WARNING   DQ pass rate 89.3% (threshold 95%)          │
│           Pipeline: Invoice Import  Run: b3f2...  [Ack] [View]  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 14. LineageViewerControl

Renders a `LineageGraph` (from Phase 6) as an interactive graph:

```
  [SourceDB.customers.email]
           │  UPPER()
           ▼
  [DWH.dim_customer.EmailAddress]
           │
           ▼
  [Report.customer_emails.email]
```

- Mouse hover on a node shows full data source / entity / field path
- Colour by data source (each source gets a unique hue)
- Click node to highlight all upstream/downstream paths

---

## 15. PipelineDesignerForm (Standalone Host)

```csharp
public class PipelineDesignerForm : Form
{
    // Layout:
    //   Left panel (200px):  PluginPaletteControl
    //   Center (flexible):   PipelineDesignerControl (canvas)
    //   Right panel (280px): PropertyPanelControl + FieldMappingGrid
    //   Bottom (150px):      Run log panel (collapses)
    //   Top:                 MenuStrip + ToolStrip

    // MenuStrip items:
    //   File: New, Open, Save, Save As, Export PNG, Close
    //   Edit: Undo, Redo, Select All, Delete
    //   View: Zoom In, Zoom Out, Zoom Fit, Toggle Properties, Toggle Palette
    //   Run:  Run, Dry Run, Stop, View Last Run
    //   Tools: Validate, Import Legacy Script, Export to Schedule

    public PipelineDefinition CurrentDefinition { get; }
    public static PipelineDefinition? ShowDialog(IDMEEditor editor, PipelineDefinition? existing = null);
}
```

---

## 16. VisualLayoutJson Schema

The `PipelineDefinition.VisualLayoutJson` field stores the `CanvasLayout` JSON so positions survive save/reload:

```json
{
  "Nodes": [
    { "Id": "n1", "StepId": "step-src", "PluginId": "beep.source.db",
      "DisplayName": "Customer DB", "Kind": 0,
      "Position": { "X": 80, "Y": 200 }, "Size": { "Width": 160, "Height": 60 } },
    { "Id": "n2", "StepId": "step-xfm", "PluginId": "beep.transform.fieldmap",
      "DisplayName": "Field Map", "Kind": 1,
      "Position": { "X": 360, "Y": 200 }, "Size": { "Width": 160, "Height": 60 } },
    { "Id": "n3", "StepId": "step-sink", "PluginId": "beep.sink.db",
      "DisplayName": "DWH", "Kind": 3,
      "Position": { "X": 640, "Y": 200 }, "Size": { "Width": 160, "Height": 60 } }
  ],
  "Edges": [
    { "Id": "e1", "FromNodeId": "n1", "ToNodeId": "n2" },
    { "Id": "e2", "FromNodeId": "n2", "ToNodeId": "n3" }
  ],
  "Zoom": 1.0,
  "Offset": { "X": 0, "Y": 0 }
}
```

---

## 17. RenderTheme

All controls accept a `RenderTheme` for light/dark mode integration with BeepThemesManager:

```csharp
public class RenderTheme
{
    public Color CanvasBackground   { get; set; } = Color.FromArgb(30, 30, 30);
    public Color GridDots           { get; set; } = Color.FromArgb(60, 60, 60);
    public Color NodeBackground     { get; set; } = Color.FromArgb(50, 50, 50);
    public Color NodeBorder         { get; set; } = Color.FromArgb(90, 90, 90);
    public Color NodeText           { get; set; } = Color.White;
    public Color EdgeColor          { get; set; } = Color.FromArgb(120, 120, 120);
    public Color SelectionColor     { get; set; } = Color.DodgerBlue;
    public Color SourceHeaderColor  { get; set; } = Color.SteelBlue;
    public Color TransformHeader    { get; set; } = Color.Teal;
    public Color ValidatorHeader    { get; set; } = Color.MediumPurple;
    public Color SinkHeaderColor    { get; set; } = Color.ForestGreen;
    public Color RunningPulse       { get; set; } = Color.Gold;
    public Color SuccessColor       { get; set; } = Color.LimeGreen;
    public Color FailureColor       { get; set; } = Color.OrangeRed;

    public static RenderTheme Dark  => new();  // above defaults
    public static RenderTheme Light => new()
    {
        CanvasBackground = Color.FromArgb(245, 245, 248),
        GridDots = Color.FromArgb(200, 200, 200),
        NodeBackground = Color.White,
        NodeBorder = Color.FromArgb(180, 180, 180),
        NodeText = Color.Black,
        EdgeColor = Color.FromArgb(130, 130, 130),
    };
}
```

---

## 18. Integration with BeepDM

### Opening the designer from within a BeepDM desktop app:

```csharp
// From any Beep form / navigator
var def = await _pipelineManager.LoadAsync(pipelineId);
using var form = new PipelineDesignerForm(_editor);
form.LoadDefinition(def);

if (form.ShowDialog() == DialogResult.OK)
{
    var updated = form.CurrentDefinition;
    await _pipelineManager.SaveAsync(updated);
}
```

### Live Monitor from task list / run history:

```csharp
var monitorForm = new RunMonitorForm(_engine, _observabilityStore);
monitorForm.AttachRun(runId);
monitorForm.Show();   // non-modal, closes when run completes
```

---

## 19. Deliverables (Implementation Checklist)

### Canvas Core
- [ ] `CanvasNode.cs`, `CanvasEdge.cs`, `CanvasLayout.cs`, `CanvasLayout.AutoLayout()`
- [ ] `CanvasRenderer.cs`, `NodeRenderer.cs`, `EdgeRenderer.cs`, `GridRenderer.cs`
- [ ] `SelectionManager.cs`, `DragDropManager.cs`, `ConnectionManager.cs`
- [ ] `DesignerState.cs` (undo/redo stack)

### Controls
- [ ] `PipelineDesignerControl.cs` (canvas host)
- [ ] `WorkflowDesignerControl.cs`
- [ ] `PluginPaletteControl.cs`
- [ ] `PropertyPanelControl.cs` (auto-gen from ParameterDefinition)
- [ ] `FieldMappingGrid.cs` (source↔target, auto-map)
- [ ] `PipelineRunMonitorControl.cs`
- [ ] `ScheduleCalendarControl.cs`
- [ ] `AlertDashboardControl.cs`
- [ ] `LineageViewerControl.cs`

### Forms
- [ ] `PipelineDesignerForm.cs`
- [ ] `WorkflowDesignerForm.cs`
- [ ] `RunMonitorForm.cs`

### Support
- [ ] `ParameterDefinition.cs` (model)
- [ ] `RenderTheme.cs`
- [ ] Add `GetParameterDefinitions()` to all built-in plugin classes (Phases 1–5)
- [ ] Wire `RunProgressEvent` in `PipelineEngine` for live monitor

---

## 20. Estimated Effort

| Component | Days |
|-----------|------|
| Canvas model (Node/Edge/Layout) | 2 |
| GDI+ rendering engine | 4 |
| PipelineDesignerControl | 5 |
| WorkflowDesignerControl | 3 |
| PluginPaletteControl | 1.5 |
| PropertyPanelControl + FieldMappingGrid | 4 |
| PipelineRunMonitorControl | 3 |
| ScheduleCalendarControl | 2 |
| AlertDashboardControl | 1.5 |
| LineageViewerControl | 2 |
| PipelineDesignerForm + other forms | 2 |
| RenderTheme + BeepThemesManager integration | 1 |
| Plugin ParameterDefinition retrofit | 3 |
| Testing + polish | 4 |
| **Total** | **38 days** |
