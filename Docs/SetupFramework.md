# Setup Framework Guide

## Overview

The Setup Framework provides wizard-based automated initialization for BeepDM applications across all platforms (Desktop, Blazor, MAUI, CLI, Web API).

## Purpose

Wizard-based automated initialization including:
- Driver provisioning
- Connection setup
- Schema creation
- Data seeding

## Entry Point

```csharp
var wizard = new SetupWizardBuilder()
    .WithId("app-setup")
    .AddStep(new DriverProvisionStep(driverOpts))
    .AddStep(new ConnectionConfigStep(connOpts))
    .AddStep(new SchemaSetupStep(schemaOpts))
    .AddStep(new SeedingStep(seedingOpts))
    .Build();

var adapter = new DesktopSetupWizardAdapter(progressCallback, completeCallback);
var report = await adapter.RunAsync(wizard, context);
```

## Steps

### 1. DriverProvisionStep
Downloads and installs required drivers from NuGet or local sources.

### 2. ConnectionConfigStep
Configures data connections with validation and testing.

### 3. SchemaSetupStep
Creates database schema based on entity definitions.

### 4. SeedingStep
Populates initial data from seeders.

## Adapters

Platform-specific adapters:
- **DesktopSetupWizardAdapter** - WinForms/WPF
- **WebSetupWizardAdapter** - ASP.NET Core
- **BlazorSetupWizardAdapter** - Blazor Server/WASM
- **MauiSetupWizardAdapter** - .NET MAUI
- **CliSetupWizardAdapter** - Command-line

## File Locations

- `DataManagementEngineStandard/SetUp/SetupWizard.cs`
- `DataManagementEngineStandard/SetUp/Steps/`
- `DataManagementEngineStandard/SetUp/Adapters/`
- `DataManagementEngineStandard/SetUp/Seeding/`

## Related Documentation

- [Core Architecture](CoreArchitecture.md)
- [Service Registration](ServiceRegistration.md)
