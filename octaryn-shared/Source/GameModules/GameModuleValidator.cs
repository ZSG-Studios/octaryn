using Octaryn.Shared.ApiExposure;
using Octaryn.Shared.FrameworkAllowlist;
using Octaryn.Shared.Host;
using Octaryn.Shared.ModuleSandbox;

namespace Octaryn.Shared.GameModules;

public static class GameModuleValidator
{
    private const HostWorkScheduleFlags AllowedScheduleFlags =
        HostWorkScheduleFlags.DeterministicOrder |
        HostWorkScheduleFlags.CanRunInParallel |
        HostWorkScheduleFlags.RequiresTickBarrier;

    private static readonly HashSet<string> s_hostNeutralFrameOrTickOwners = new(StringComparer.Ordinal)
    {
        HostScheduleIds.FrameOrTickOwner
    };

    private static readonly HashSet<string> s_hostNeutralCommitBarriers = new(StringComparer.Ordinal)
    {
        HostScheduleIds.FrameOrTickEndBarrier
    };

    private static readonly HashSet<string> s_allowedHostReadResources = new(StringComparer.Ordinal)
    {
        HostApiIds.Frame
    };

    private static readonly HashSet<string> s_allowedHostWriteResources = new(StringComparer.Ordinal)
    {
        HostApiIds.Commands
    };

    private static readonly HashSet<string> s_contentKinds = new(StringComparer.Ordinal)
    {
        "block",
        "item",
        "material",
        "recipe",
        "tag",
        "loot_table",
        "feature",
        "biome",
        "rule"
    };

    private static readonly HashSet<string> s_assetKinds = new(StringComparer.Ordinal)
    {
        "atlas",
        "blockstate",
        "model",
        "shader",
        "texture",
        "ui",
        "audio"
    };

    public static ModuleValidationReport Validate(GameModuleManifest? manifest)
    {
        var report = new ModuleValidationReport();
        if (manifest is null)
        {
            report.AddError("module.manifest.required", "Module manifest is required.");
            return report;
        }

        var compatibility = manifest.Compatibility;
        if (compatibility is null)
        {
            report.AddError("module.compatibility.required", "Module compatibility declaration is required.");
            compatibility = new GameModuleCompatibility(string.Empty, string.Empty, string.Empty, SupportsMultiplayer: false);
        }

        var requiredCapabilities = manifest.RequiredCapabilities ?? [];
        var requestedHostApis = manifest.RequestedHostApis ?? [];
        var requestedRuntimePackages = manifest.RequestedRuntimePackages ?? [];
        var requestedBuildPackages = manifest.RequestedBuildPackages ?? [];
        var requestedFrameworkApiGroups = manifest.RequestedFrameworkApiGroups ?? [];
        var moduleDependencies = manifest.ModuleDependencies ?? [];
        var contentDeclarations = manifest.ContentDeclarations ?? [];
        var assetDeclarations = manifest.AssetDeclarations ?? [];
        var schedule = manifest.Schedule ?? new GameModuleScheduleDeclaration([]);
        var scheduledSystems = schedule.Systems ?? [];

        RequireText(report, manifest.ModuleId, "module.id.required", "Module ID is required.");
        RequireText(report, manifest.DisplayName, "module.display_name.required", "Display name is required.");
        RequireText(report, manifest.Version, "module.version.required", "Module version is required.");
        RequireText(report, manifest.OctarynApiVersion, "module.api_version.required", "Octaryn API version is required.");
        RequireText(report, compatibility.MinimumHostApiVersion, "module.compatibility.minimum_host_api.required", "Minimum host API version is required.");
        RequireText(report, compatibility.MaximumHostApiVersion, "module.compatibility.maximum_host_api.required", "Maximum host API version is required.");
        RequireText(report, compatibility.SaveCompatibilityId, "module.compatibility.save_id.required", "Save compatibility ID is required.");
        RequireVersion(report, manifest.Version, "module.version.invalid", "Module version is invalid.");
        RequireVersion(report, manifest.OctarynApiVersion, "module.api_version.invalid", "Octaryn API version is invalid.");
        RequireVersion(report, compatibility.MinimumHostApiVersion, "module.compatibility.minimum_host_api.invalid", "Minimum host API version is invalid.");
        RequireVersion(report, compatibility.MaximumHostApiVersion, "module.compatibility.maximum_host_api.invalid", "Maximum host API version is invalid.");
        RequireVersionRange(
            report,
            compatibility.MinimumHostApiVersion,
            compatibility.MaximumHostApiVersion,
            "module.compatibility.host_api_range.invalid",
            "Host API compatibility range is invalid.");
        RequireUnique(report, requiredCapabilities, "module.capability.duplicate", "Required capability is duplicated.");
        RequireUnique(report, requestedHostApis, "module.host_api.duplicate", "Requested host API is duplicated.");
        RequireUnique(report, requestedRuntimePackages, "module.runtime_package.duplicate", "Requested runtime package is duplicated.");
        RequireUnique(report, requestedBuildPackages, "module.build_package.duplicate", "Requested build package is duplicated.");
        RequireUnique(report, requestedFrameworkApiGroups, "module.framework_api.duplicate", "Requested framework API group is duplicated.");
        RequireUnique(
            report,
            scheduledSystems.Select(system => system.SystemId),
            "module.schedule.system.duplicate",
            "Scheduled system is duplicated.");
        RequireUnique(
            report,
            moduleDependencies.Select(dependency => dependency.ModuleId),
            "module.dependency.duplicate",
            "Module dependency is duplicated.");
        RequireUnique(
            report,
            contentDeclarations.Select(content => content.ContentId),
            "module.content.duplicate",
            "Content declaration is duplicated.");
        RequireUnique(
            report,
            contentDeclarations.Select(content => content.RelativePath),
            "module.content.path.duplicate",
            "Content declaration path is duplicated.");
        RequireUnique(
            report,
            assetDeclarations.Select(asset => asset.AssetId),
            "module.asset.duplicate",
            "Asset declaration is duplicated.");
        RequireUnique(
            report,
            assetDeclarations.Select(asset => asset.RelativePath),
            "module.asset.path.duplicate",
            "Asset declaration path is duplicated.");
        RequireUnique(
            report,
            contentDeclarations.Select(content => content.ContentId)
                .Concat(assetDeclarations.Select(asset => asset.AssetId))
                .Concat(scheduledSystems.Select(system => system.SystemId)),
            "module.declaration.id.duplicate",
            "Module declaration ID is duplicated across content, assets, and scheduled systems.");
        RequireModuleOwnedDeclarations(report, manifest.ModuleId, contentDeclarations, assetDeclarations, scheduledSystems);
        RequireDeclarations(report, moduleDependencies);
        RequireDeclarations(report, contentDeclarations);
        RequireDeclarations(report, assetDeclarations);
        RequireDeclarations(report, scheduledSystems);
        RequireScheduleGraph(report, scheduledSystems);
        RequireScheduleResourceConflicts(report, scheduledSystems);
        RequireAllowed(
            report,
            requiredCapabilities,
            ModuleCapabilityAllowlist.IsAllowed,
            "module.capability.not_allowed",
            "Required capability is not exposed by the host API.");
        RequireAllowed(
            report,
            requestedHostApis,
            HostApiAllowlist.IsAllowed,
            "module.host_api.not_allowed",
            "Requested host API is not exposed.");
        RequireAllowed(
            report,
            requestedRuntimePackages,
            ModulePackageAllowlist.IsAllowed,
            "module.runtime_package.not_allowed",
            "Requested runtime package is not allowed for game modules.");
        RequireAllowed(
            report,
            requestedBuildPackages,
            ModuleBuildPackageAllowlist.IsAllowed,
            "module.build_package.not_allowed",
            "Requested build package is not allowed for game modules.");
        RequireAllowed(
            report,
            requestedFrameworkApiGroups,
            FrameworkApiGroupAllowlist.IsAllowed,
            "module.framework_api.not_allowed",
            "Requested framework API group is not allowed for game modules.");
        RequireDenied(
            report,
            requestedFrameworkApiGroups,
            DeniedFrameworkApiGroups.Values,
            "module.framework_api.denied",
            "Requested framework API group is explicitly denied for game modules.");
        if (HasScheduledRead(scheduledSystems, HostApiIds.Frame) &&
            !requestedHostApis.Contains(HostApiIds.Frame, StringComparer.Ordinal))
        {
            report.AddError(
                "module.schedule.frame.read.required",
                $"Scheduled frame reads require host API: {HostApiIds.Frame}");
        }

        if (requestedHostApis.Contains(HostApiIds.Commands, StringComparer.Ordinal) &&
            !HasScheduledWrite(scheduledSystems, HostApiIds.Commands))
        {
            report.AddError(
                "module.schedule.commands.write.required",
                $"Requested command access must be declared as a scheduled write resource: {HostApiIds.Commands}");
        }

        if ((requestedHostApis.Contains(HostApiIds.Commands, StringComparer.Ordinal) ||
             HasScheduledWrite(scheduledSystems, HostApiIds.Commands)) &&
            !requiredCapabilities.Contains(ModuleCapabilityIds.WorldBlockEdits, StringComparer.Ordinal))
        {
            report.AddError(
                "module.capability.world_block_edits.required",
                $"Block edit command access requires capability: {ModuleCapabilityIds.WorldBlockEdits}");
        }

        return report;
    }

    private static void RequireText(ModuleValidationReport report, string value, string code, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            report.AddError(code, message);
        }
    }

    private static void RequireUnique(
        ModuleValidationReport report,
        IEnumerable<string> values,
        string code,
        string message)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                report.AddError(code, message);
                continue;
            }

            if (!seen.Add(value))
            {
                report.AddError(code, $"{message} Value: {value}");
            }
        }
    }

    private static void RequireDeclarations(
        ModuleValidationReport report,
        IReadOnlyList<GameModuleDependency> dependencies)
    {
        foreach (var dependency in dependencies)
        {
            RequireText(report, dependency.ModuleId, "module.dependency.id.required", "Module dependency ID is required.");
            RequireText(report, dependency.MinimumVersion, "module.dependency.minimum_version.required", "Module dependency minimum version is required.");
            RequireText(report, dependency.MaximumVersion, "module.dependency.maximum_version.required", "Module dependency maximum version is required.");
            RequireVersion(report, dependency.MinimumVersion, "module.dependency.minimum_version.invalid", "Module dependency minimum version is invalid.");
            RequireVersion(report, dependency.MaximumVersion, "module.dependency.maximum_version.invalid", "Module dependency maximum version is invalid.");
            RequireVersionRange(
                report,
                dependency.MinimumVersion,
                dependency.MaximumVersion,
                "module.dependency.version_range.invalid",
                "Module dependency version range is invalid.");
        }
    }

    private static void RequireDeclarations(
        ModuleValidationReport report,
        IReadOnlyList<GameModuleContentDeclaration> contents)
    {
        foreach (var content in contents)
        {
            RequireText(report, content.ContentId, "module.content.id.required", "Content ID is required.");
            RequireText(report, content.ContentKind, "module.content.kind.required", "Content kind is required.");
            RequireText(report, content.RelativePath, "module.content.path.required", "Content path is required.");
            RequireVocabulary(report, content.ContentKind, s_contentKinds, "module.content.kind.invalid", "Content kind is not recognized.");
            RequireSafeRelativePath(report, content.RelativePath, "module.content.path.invalid", "Content path must be a safe relative path.");
            if (!string.IsNullOrWhiteSpace(content.RelativePath) &&
                !content.RelativePath.StartsWith("Data/", StringComparison.Ordinal))
            {
                report.AddError(
                    "module.content.path.owner.invalid",
                    $"Content declarations must live under Data/. Value: {content.RelativePath}");
            }
        }
    }

    private static void RequireDeclarations(
        ModuleValidationReport report,
        IReadOnlyList<GameModuleAssetDeclaration> assets)
    {
        foreach (var asset in assets)
        {
            RequireText(report, asset.AssetId, "module.asset.id.required", "Asset ID is required.");
            RequireText(report, asset.AssetKind, "module.asset.kind.required", "Asset kind is required.");
            RequireText(report, asset.RelativePath, "module.asset.path.required", "Asset path is required.");
            RequireVocabulary(report, asset.AssetKind, s_assetKinds, "module.asset.kind.invalid", "Asset kind is not recognized.");
            RequireSafeRelativePath(report, asset.RelativePath, "module.asset.path.invalid", "Asset path must be a safe relative path.");
            if (!string.IsNullOrWhiteSpace(asset.RelativePath) &&
                !asset.RelativePath.StartsWith("Assets/", StringComparison.Ordinal) &&
                !asset.RelativePath.StartsWith("Shaders/", StringComparison.Ordinal))
            {
                report.AddError(
                    "module.asset.path.owner.invalid",
                    $"Asset declarations must live under Assets/ or Shaders/. Value: {asset.RelativePath}");
            }
        }
    }

    private static void RequireDeclarations(
        ModuleValidationReport report,
        IReadOnlyList<ScheduledSystemDeclaration> systems)
    {
        foreach (var system in systems)
        {
            RequireText(report, system.SystemId, "module.schedule.system_id.required", "Scheduled system ID is required.");
            RequireText(report, system.FrameOrTickOwner, "module.schedule.owner.required", "Scheduled system frame/tick owner is required.");
            RequireText(report, system.CommitBarrier, "module.schedule.commit_barrier.required", "Scheduled system commit barrier is required.");
            RequireVocabulary(
                report,
                system.FrameOrTickOwner,
                s_hostNeutralFrameOrTickOwners,
                "module.schedule.owner.invalid",
                "Scheduled system owner must be host-neutral.");
            RequireVocabulary(
                report,
                system.CommitBarrier,
                s_hostNeutralCommitBarriers,
                "module.schedule.commit_barrier.invalid",
                "Scheduled system commit barrier must be host-neutral.");
            if (!Enum.IsDefined(system.Phase))
            {
                report.AddError("module.schedule.phase.invalid", $"Scheduled system phase is invalid. System: {system.SystemId}");
            }

            if ((system.Flags & ~AllowedScheduleFlags) != 0)
            {
                report.AddError("module.schedule.flags.invalid", $"Scheduled system flags are invalid. System: {system.SystemId}");
            }

            RequireResourceAccess(report, system.SystemId, system.Reads ?? [], "read");
            RequireResourceAccess(report, system.SystemId, system.Writes ?? [], "write");
            RequireUnique(
                report,
                (system.Reads ?? []).Select(resource => resource.ResourceId),
                "module.schedule.read.resource_duplicate",
                $"Scheduled system has duplicated read resource. System: {system.SystemId}");
            RequireUnique(
                report,
                (system.Writes ?? []).Select(resource => resource.ResourceId),
                "module.schedule.write.resource_duplicate",
                $"Scheduled system has duplicated write resource. System: {system.SystemId}");
            RequireNoResourceOverlap(report, system);
            RequireUnique(
                report,
                system.RunsAfter ?? [],
                "module.schedule.runs_after.duplicate",
                $"Scheduled system has duplicated RunsAfter dependency. System: {system.SystemId}");
            RequireUnique(
                report,
                system.RunsBefore ?? [],
                "module.schedule.runs_before.duplicate",
                $"Scheduled system has duplicated RunsBefore dependency. System: {system.SystemId}");
        }
    }

    private static void RequireModuleOwnedDeclarations(
        ModuleValidationReport report,
        string moduleId,
        IReadOnlyList<GameModuleContentDeclaration> contents,
        IReadOnlyList<GameModuleAssetDeclaration> assets,
        IReadOnlyList<ScheduledSystemDeclaration> systems)
    {
        if (string.IsNullOrWhiteSpace(moduleId))
        {
            return;
        }

        foreach (var content in contents)
        {
            RequireModuleOwnedId(report, moduleId, content.ContentId, "module.content.id.owner.invalid", "Content ID");
        }

        foreach (var asset in assets)
        {
            RequireModuleOwnedId(report, moduleId, asset.AssetId, "module.asset.id.owner.invalid", "Asset ID");
        }

        foreach (var system in systems)
        {
            RequireModuleOwnedId(report, moduleId, system.SystemId, "module.schedule.system_id.owner.invalid", "Scheduled system ID");
            foreach (var resource in system.Reads ?? [])
            {
                if (!string.IsNullOrWhiteSpace(resource.ResourceId) &&
                    resource.ResourceId.StartsWith("host.", StringComparison.Ordinal))
                {
                    if (!s_allowedHostReadResources.Contains(resource.ResourceId))
                    {
                        report.AddError(
                            "module.schedule.read.host_resource.invalid",
                            $"Scheduled system reads an unexposed host resource. System: {system.SystemId} Resource: {resource.ResourceId}");
                    }

                    continue;
                }

                RequireModuleOwnedId(report, moduleId, resource.ResourceId, "module.schedule.read.resource_id.owner.invalid", "Scheduled read resource ID");
            }

            foreach (var resource in system.Writes ?? [])
            {
                if (!string.IsNullOrWhiteSpace(resource.ResourceId) &&
                    resource.ResourceId.StartsWith("host.", StringComparison.Ordinal))
                {
                    if (!s_allowedHostWriteResources.Contains(resource.ResourceId))
                    {
                        report.AddError(
                            "module.schedule.write.host_resource.invalid",
                            $"Scheduled system writes an unexposed host resource. System: {system.SystemId} Resource: {resource.ResourceId}");
                    }

                    continue;
                }

                RequireModuleOwnedId(report, moduleId, resource.ResourceId, "module.schedule.write.resource_id.owner.invalid", "Scheduled write resource ID");
            }
        }
    }

    private static bool HasScheduledWrite(
        IReadOnlyList<ScheduledSystemDeclaration> systems,
        string resourceId)
    {
        return systems.Any(system => (system.Writes ?? [])
            .Any(resource => resource.ResourceId == resourceId && resource.Mode == ScheduledAccessMode.Write));
    }

    private static bool HasScheduledRead(
        IReadOnlyList<ScheduledSystemDeclaration> systems,
        string resourceId)
    {
        return systems.Any(system => (system.Reads ?? [])
            .Any(resource => resource.ResourceId == resourceId && resource.Mode == ScheduledAccessMode.Read));
    }

    private static void RequireModuleOwnedId(
        ModuleValidationReport report,
        string moduleId,
        string value,
        string code,
        string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!value.StartsWith($"{moduleId}.", StringComparison.Ordinal))
        {
            report.AddError(code, $"{label} must be module-owned. Module: {moduleId} Value: {value}");
        }
    }

    private static void RequireResourceAccess(
        ModuleValidationReport report,
        string systemId,
        IReadOnlyList<ScheduledResourceAccess> resources,
        string direction)
    {
        foreach (var resource in resources)
        {
            RequireText(
                report,
                resource.ResourceId,
                $"module.schedule.{direction}.resource_id.required",
                $"Scheduled system {direction} resource ID is required. System: {systemId}");
            if (resource.Mode == 0)
            {
                report.AddError(
                    $"module.schedule.{direction}.mode.required",
                    $"Scheduled system {direction} resource mode is required. System: {systemId}");
                continue;
            }

            if (!Enum.IsDefined(resource.Mode))
            {
                report.AddError(
                    $"module.schedule.{direction}.mode.invalid",
                    $"Scheduled system {direction} resource mode is invalid. System: {systemId}");
                continue;
            }

            var expectedMode = direction == "read"
                ? ScheduledAccessMode.Read
                : ScheduledAccessMode.Write;
            if (resource.Mode != expectedMode)
            {
                report.AddError(
                    $"module.schedule.{direction}.mode.mismatch",
                    $"Scheduled system {direction} resource mode must be {expectedMode}. System: {systemId} Resource: {resource.ResourceId}");
            }
        }
    }

    private static void RequireNoResourceOverlap(
        ModuleValidationReport report,
        ScheduledSystemDeclaration system)
    {
        var reads = (system.Reads ?? [])
            .Select(resource => resource.ResourceId)
            .Where(resourceId => !string.IsNullOrWhiteSpace(resourceId))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var resourceId in (system.Writes ?? []).Select(resource => resource.ResourceId))
        {
            if (reads.Contains(resourceId))
            {
                report.AddError(
                    "module.schedule.resource.read_write_overlap",
                    $"Scheduled system cannot declare the same resource as read and write. System: {system.SystemId} Resource: {resourceId}");
            }
        }
    }

    private static void RequireScheduleResourceConflicts(
        ModuleValidationReport report,
        IReadOnlyList<ScheduledSystemDeclaration> systems)
    {
        var orderedPairs = BuildOrderedSchedulePairs(systems);
        for (var leftIndex = 0; leftIndex < systems.Count; leftIndex++)
        {
            for (var rightIndex = leftIndex + 1; rightIndex < systems.Count; rightIndex++)
            {
                var left = systems[leftIndex];
                var right = systems[rightIndex];
                if (string.IsNullOrWhiteSpace(left.SystemId) || string.IsNullOrWhiteSpace(right.SystemId))
                {
                    continue;
                }

                if (IsOrdered(orderedPairs, left.SystemId, right.SystemId))
                {
                    continue;
                }

                if (ConflictingResources(left, right).FirstOrDefault() is { } resourceId)
                {
                    report.AddError(
                        "module.schedule.resource.conflict_unordered",
                        $"Scheduled systems with write conflicts must declare ordering. Left: {left.SystemId} Right: {right.SystemId} Resource: {resourceId}");
                }
            }
        }
    }

    private static IEnumerable<string> ConflictingResources(
        ScheduledSystemDeclaration left,
        ScheduledSystemDeclaration right)
    {
        var leftReads = ResourceIds(left.Reads);
        var leftWrites = ResourceIds(left.Writes);
        var rightReads = ResourceIds(right.Reads);
        var rightWrites = ResourceIds(right.Writes);

        foreach (var resourceId in leftWrites.Intersect(rightWrites, StringComparer.Ordinal)
            .Concat(leftWrites.Intersect(rightReads, StringComparer.Ordinal))
            .Concat(rightWrites.Intersect(leftReads, StringComparer.Ordinal)))
        {
            yield return resourceId;
        }
    }

    private static HashSet<string> ResourceIds(IReadOnlyList<ScheduledResourceAccess>? resources)
    {
        return (resources ?? [])
            .Select(resource => resource.ResourceId)
            .Where(resourceId => !string.IsNullOrWhiteSpace(resourceId))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static HashSet<(string Before, string After)> BuildOrderedSchedulePairs(
        IReadOnlyList<ScheduledSystemDeclaration> systems)
    {
        var systemIds = systems
            .Select(system => system.SystemId)
            .Where(systemId => !string.IsNullOrWhiteSpace(systemId))
            .ToHashSet(StringComparer.Ordinal);
        var directEdges = systemIds.ToDictionary(systemId => systemId, _ => new List<string>(), StringComparer.Ordinal);

        foreach (var system in systems)
        {
            if (string.IsNullOrWhiteSpace(system.SystemId) || !directEdges.ContainsKey(system.SystemId))
            {
                continue;
            }

            foreach (var dependency in system.RunsAfter ?? [])
            {
                if (!string.IsNullOrWhiteSpace(dependency) && directEdges.TryGetValue(dependency, out var dependencyEdges))
                {
                    dependencyEdges.Add(system.SystemId);
                }
            }

            foreach (var dependency in system.RunsBefore ?? [])
            {
                if (!string.IsNullOrWhiteSpace(dependency) && directEdges.ContainsKey(dependency))
                {
                    directEdges[system.SystemId].Add(dependency);
                }
            }
        }

        var orderedPairs = new HashSet<(string Before, string After)>();
        foreach (var systemId in systemIds)
        {
            AddReachablePairs(systemId, systemId, directEdges, orderedPairs, []);
        }

        return orderedPairs;
    }

    private static void AddReachablePairs(
        string root,
        string current,
        IReadOnlyDictionary<string, List<string>> edges,
        HashSet<(string Before, string After)> orderedPairs,
        HashSet<string> visited)
    {
        if (!visited.Add(current))
        {
            return;
        }

        foreach (var next in edges[current])
        {
            orderedPairs.Add((root, next));
            AddReachablePairs(root, next, edges, orderedPairs, visited);
        }
    }

    private static bool IsOrdered(
        HashSet<(string Before, string After)> orderedPairs,
        string left,
        string right)
    {
        return orderedPairs.Contains((left, right)) || orderedPairs.Contains((right, left));
    }

    private static void RequireScheduleGraph(
        ModuleValidationReport report,
        IReadOnlyList<ScheduledSystemDeclaration> systems)
    {
        var systemIds = systems
            .Select(system => system.SystemId)
            .Where(systemId => !string.IsNullOrWhiteSpace(systemId))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var system in systems)
        {
            RequireScheduleDependencies(
                report,
                system.SystemId,
                system.RunsAfter ?? [],
                systemIds,
                "runs_after");
            RequireScheduleDependencies(
                report,
                system.SystemId,
                system.RunsBefore ?? [],
                systemIds,
                "runs_before");
        }

        var edges = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var systemId in systemIds)
        {
            edges[systemId] = [];
        }

        foreach (var system in systems)
        {
            if (string.IsNullOrWhiteSpace(system.SystemId) || !edges.ContainsKey(system.SystemId))
            {
                continue;
            }

            foreach (var dependency in system.RunsAfter ?? [])
            {
                if (!string.IsNullOrWhiteSpace(dependency) &&
                    edges.TryGetValue(dependency, out var dependencyEdges))
                {
                    dependencyEdges.Add(system.SystemId);
                }
            }

            foreach (var dependency in system.RunsBefore ?? [])
            {
                if (!string.IsNullOrWhiteSpace(dependency) && edges.ContainsKey(dependency))
                {
                    edges[system.SystemId].Add(dependency);
                }
            }
        }

        RequireAcyclicScheduleGraph(report, edges);
    }

    private static void RequireScheduleDependencies(
        ModuleValidationReport report,
        string systemId,
        IReadOnlyList<string> dependencies,
        IReadOnlySet<string> systemIds,
        string direction)
    {
        foreach (var dependency in dependencies)
        {
            if (string.IsNullOrWhiteSpace(dependency))
            {
                continue;
            }

            if (dependency == systemId)
            {
                report.AddError(
                    $"module.schedule.{direction}.self",
                    $"Scheduled system cannot depend on itself. System: {systemId}");
                continue;
            }

            if (!systemIds.Contains(dependency))
            {
                report.AddError(
                    $"module.schedule.{direction}.unknown",
                    $"Scheduled system dependency is not declared. System: {systemId} Dependency: {dependency}");
            }
        }
    }

    private static void RequireAcyclicScheduleGraph(
        ModuleValidationReport report,
        IReadOnlyDictionary<string, List<string>> edges)
    {
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);

        foreach (var systemId in edges.Keys)
        {
            if (VisitScheduleNode(systemId, edges, visiting, visited))
            {
                report.AddError(
                    "module.schedule.graph.cycle",
                    $"Scheduled system dependency graph contains a cycle. System: {systemId}");
                return;
            }
        }
    }

    private static bool VisitScheduleNode(
        string systemId,
        IReadOnlyDictionary<string, List<string>> edges,
        HashSet<string> visiting,
        HashSet<string> visited)
    {
        if (visited.Contains(systemId))
        {
            return false;
        }

        if (!visiting.Add(systemId))
        {
            return true;
        }

        foreach (var next in edges[systemId])
        {
            if (VisitScheduleNode(next, edges, visiting, visited))
            {
                return true;
            }
        }

        visiting.Remove(systemId);
        visited.Add(systemId);
        return false;
    }

    private static void RequireVersion(ModuleValidationReport report, string value, string code, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!Version.TryParse(value, out _))
        {
            report.AddError(code, $"{message} Value: {value}");
        }
    }

    private static void RequireVersionRange(
        ModuleValidationReport report,
        string minimumVersion,
        string maximumVersion,
        string code,
        string message)
    {
        if (!Version.TryParse(minimumVersion, out var minimum) ||
            !Version.TryParse(maximumVersion, out var maximum))
        {
            return;
        }

        if (minimum > maximum)
        {
            report.AddError(code, $"{message} Minimum: {minimumVersion} Maximum: {maximumVersion}");
        }
    }

    private static void RequireVocabulary(
        ModuleValidationReport report,
        string value,
        IReadOnlySet<string> vocabulary,
        string code,
        string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!vocabulary.Contains(value))
        {
            report.AddError(code, $"{message} Value: {value}");
        }
    }

    private static void RequireSafeRelativePath(ModuleValidationReport report, string path, string code, string message)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (path.StartsWith('/'.ToString(), StringComparison.Ordinal) ||
            path.StartsWith('\\'.ToString(), StringComparison.Ordinal) ||
            path.EndsWith('/'.ToString(), StringComparison.Ordinal) ||
            path.Contains('\\', StringComparison.Ordinal) ||
            path.Contains(':', StringComparison.Ordinal) ||
            path.Split('/').Any(segment => string.IsNullOrWhiteSpace(segment) ||
                segment == "." ||
                segment == ".."))
        {
            report.AddError(code, $"{message} Value: {path}");
        }
    }

    private static void RequireAllowed(
        ModuleValidationReport report,
        IReadOnlyList<string> values,
        Func<string, bool> isAllowed,
        string code,
        string message)
    {
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (!isAllowed(value))
            {
                report.AddError(code, $"{message} Value: {value}");
            }
        }
    }

    private static void RequireDenied(
        ModuleValidationReport report,
        IReadOnlyList<string> values,
        IReadOnlyList<string> deniedValues,
        string code,
        string message)
    {
        var denied = new HashSet<string>(deniedValues, StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (denied.Contains(value))
            {
                report.AddError(code, $"{message} Value: {value}");
            }
        }
    }
}
