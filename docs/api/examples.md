# API Examples

These examples show the current contract shape. They are intentionally small and focus on boundaries.

## Register A Module

```csharp
using Octaryn.Shared.ApiExposure;
using Octaryn.Shared.FrameworkAllowlist;
using Octaryn.Shared.GameModules;
using Octaryn.Shared.Host;

public sealed class ExampleModuleRegistration : IGameModuleRegistration
{
    public GameModuleManifest Manifest { get; } = new(
        ModuleId: "example.demo",
        DisplayName: "Example Demo",
        Version: "0.1.0",
        OctarynApiVersion: "0.1.0",
        RequiredCapabilities:
        [
            ModuleCapabilityIds.GameplayRules
        ],
        RequestedHostApis:
        [
            HostApiIds.Frame
        ],
        RequestedRuntimePackages: [],
        RequestedBuildPackages: [],
        RequestedFrameworkApiGroups:
        [
            FrameworkApiGroupIds.BclPrimitives,
            FrameworkApiGroupIds.BclCollections
        ],
        ModuleDependencies: [],
        ContentDeclarations: [],
        AssetDeclarations: [],
        Schedule: new GameModuleScheduleDeclaration(
        [
            new ScheduledSystemDeclaration(
                SystemId: "example.demo.frame_tick",
                Phase: HostWorkPhase.Gameplay,
                FrameOrTickOwner: HostScheduleIds.FrameOrTickOwner,
                Reads:
                [
                    new ScheduledResourceAccess(HostApiIds.Frame, ScheduledAccessMode.Read)
                ],
                Writes: [],
                RunsAfter: [],
                RunsBefore: [],
                Flags: HostWorkScheduleFlags.DeterministicOrder,
                CommitBarrier: HostScheduleIds.FrameOrTickEndBarrier)
        ]),
        Compatibility: new GameModuleCompatibility(
            MinimumHostApiVersion: "0.1.0",
            MaximumHostApiVersion: "0.1.0",
            SaveCompatibilityId: "example.demo.save.v0",
            SupportsMultiplayer: false));

    public IGameModuleInstance CreateInstance(ModuleHostContext context)
    {
        return new ExampleModule();
    }
}
```

## Implement A Module Instance

```csharp
using Octaryn.Shared.GameModules;

public sealed class ExampleModule : IGameModuleInstance
{
    public void Tick(in ModuleFrameContext frame)
    {
        double deltaSeconds = frame.DeltaSeconds;
        ulong frameIndex = frame.FrameIndex;
    }

    public void Dispose()
    {
    }
}
```

## Validate A Manifest

```csharp
using Octaryn.Shared.GameModules;

IGameModuleRegistration registration = new ExampleModuleRegistration();
ModuleValidationReport report = GameModuleValidator.Validate(registration.Manifest);

if (!report.IsValid)
{
    foreach (ModuleValidationIssue issue in report.Issues)
    {
        Console.Error.WriteLine($"{issue.Severity}: {issue.Code} - {issue.Message}");
    }
}
```

## Request Command Access

Command access is capability-scoped. A module that writes host commands must request `HostApiIds.Commands`, declare a scheduled write to that host API, and request the capability that permits the command family.

```csharp
RequiredCapabilities:
[
    ModuleCapabilityIds.WorldBlockEdits
],
RequestedHostApis:
[
    HostApiIds.Frame,
    HostApiIds.Commands
],
Schedule: new GameModuleScheduleDeclaration(
[
    new ScheduledSystemDeclaration(
        SystemId: "example.demo.block_edits",
        Phase: HostWorkPhase.Gameplay,
        FrameOrTickOwner: HostScheduleIds.FrameOrTickOwner,
        Reads:
        [
            new ScheduledResourceAccess(HostApiIds.Frame, ScheduledAccessMode.Read)
        ],
        Writes:
        [
            new ScheduledResourceAccess(HostApiIds.Commands, ScheduledAccessMode.Write)
        ],
        RunsAfter: [],
        RunsBefore: [],
        Flags: HostWorkScheduleFlags.DeterministicOrder | HostWorkScheduleFlags.RequiresTickBarrier,
        CommitBarrier: HostScheduleIds.FrameOrTickEndBarrier)
])
```

## Read Snapshot Contracts

Shared snapshot structs are versioned and packed so native and managed owners can agree on layout.

```csharp
using Octaryn.Shared.Networking;

static bool IsSupported(in ServerSnapshotHeader header)
{
    return header.Version == ServerSnapshotHeader.VersionValue &&
           header.Size == ServerSnapshotHeader.SizeValue;
}
```

Transport code decides how bytes move. Shared contracts define what the bytes mean.
