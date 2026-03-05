---
name: shared-context-plugin-system
description: Detailed guidance for PluginSystem components used by SharedContextAssemblyHandler and SharedContextManager. Use when working on plugin isolation, lifecycle, versioning, messaging, service orchestration, health monitoring, registry, and install flows.
---

# Shared Context PluginSystem

Use this skill when modifying classes under `AssemblyHandler/PluginSystem`.

## Additional Resources
- End-to-end scenarios: [reference.md](reference.md)

## Key Classes
- `SharedContextManager`
- `SharedContextLoadContext`
- `PluginRegistry`
- `PluginInstaller`
- `PluginIsolationManager`
- `PluginLifecycleManager`
- `PluginVersionManager`
- `PluginMessageBus`
- `PluginServiceManager`
- `PluginHealthMonitor`
- `NuggetPluginLoader`

## Architectural Intent
- Single shared context mode for maximum interop, with support for alternate context modes.
- Event-driven lifecycle visibility (`NuggetLoaded`, `NuggetUnloaded`, `PluginLoaded`, `PluginUnloaded`).
- Thread-safe discovered-item snapshots and registration caches.
- Integration with assembly resolver and pre-loaded assembly registration.

## Safe Change Rules
1. Preserve lock/thread-safety semantics around shared collections.
2. Keep manager responsibilities separated (isolation vs lifecycle vs versioning vs service bus).
3. Maintain backward-compatible event contracts used by handler consumers.
4. Ensure plugin registry/install state remains consistent across process restarts.

## Common Pitfalls
- Mixing context ownership logic between `SharedContextManager` and caller handlers.
- Registering assemblies without tracking source context metadata.
- Breaking auto-registration of preloaded assemblies and causing binding conflicts.
- Introducing blocking/event recursion patterns in plugin lifecycle callbacks.

## Verification Checklist
- Plugin load/unload events fire in expected order.
- Shared context contains expected assemblies after install/load operations.
- Registry reflects installed/uninstalled plugin state transitions.
- Message bus and service manager continue to resolve plugin services correctly.

