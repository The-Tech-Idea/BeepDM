# Workflow Engine Guide

## Overview

The Workflow Engine provides a flexible system for defining and executing multi-step business processes with actions, triggers, and retry policies.

## Core Concepts

- **WorkFlow** - A defined sequence of steps
- **WorkFlowStep** - Individual step in a workflow
- **WorkFlowAction** - Executable action within a step
- **WorkFlowTrigger** - Event that initiates a workflow
- **WorkFlowRetryPolicy** - Retry configuration for failed steps

## Defining a Workflow

```csharp
var workflow = new WorkFlow
{
    Name = "OrderProcessing",
    Steps = new List<WorkFlowStep>
    {
        new WorkFlowStep
        {
            Name = "ValidateOrder",
            Action = new ValidateOrderAction(),
            OnSuccess = "ProcessPayment",
            OnFailure = "NotifyError"
        },
        new WorkFlowStep
        {
            Name = "ProcessPayment",
            Action = new ProcessPaymentAction(),
            OnSuccess = "FulfillOrder",
            OnFailure = "RefundPayment"
        },
        new WorkFlowStep
        {
            Name = "FulfillOrder",
            Action = new FulfillOrderAction()
        }
    }
};
```

## Triggers

```csharp
// Event-based trigger
workflow.Triggers.Add(new WorkFlowTrigger
{
    TriggerType = TriggerType.Event,
    EventName = "OrderCreated"
});

// Scheduled trigger
workflow.Triggers.Add(new WorkFlowTrigger
{
    TriggerType = TriggerType.Schedule,
    CronExpression = "0 0 * * *" // Daily at midnight
});
```

## Execution

```csharp
var engine = new WorkFlowEditor(editor);
var result = await engine.ExecuteAsync(workflow, new WorkFlowContext
{
    Data = order,
    Parameters = new Dictionary<string, object> { ["UserId"] = userId }
});

if (result.Success)
{
    Console.WriteLine($"Workflow completed in {result.Duration}");
}
else
{
    Console.WriteLine($"Failed at step: {result.FailedStep}");
}
```

## Retry Policies

```csharp
workflow.Steps[0].RetryPolicy = new WorkFlowRetryPolicy
{
    MaxRetries = 3,
    Delay = TimeSpan.FromSeconds(5),
    BackoffMultiplier = 2.0,
    RetryOn = new[] { typeof(TimeoutException), typeof(IOException) }
};
```

## File Locations

- `DataManagementEngineStandard/Workflow/`
- `DataManagementModelsStandard/Workflow/`
- `DataManagementEngineStandard/Editor/WorkFlowEditor.cs`

## Related Documentation

- [Core Architecture](CoreArchitecture.md)
- [Rules Engine](RulesEngine.md)
- [ETL Operations](ETL.md)
