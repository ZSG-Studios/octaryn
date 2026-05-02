using Octaryn.Basegame.Module;
using Octaryn.Shared.ApiExposure;
using Octaryn.Shared.FrameworkAllowlist;
using Octaryn.Shared.GameModules;
using Octaryn.Shared.Host;
using System.Text.Json;
using System.Text.Json.Nodes;

return ModuleManifestProbe.Run(args);

internal static class ModuleManifestProbe
{
    public static int Run(IReadOnlyList<string> args)
    {
        var selfTestErrors = RunSelfTests();
        if (selfTestErrors.Count > 0)
        {
            foreach (var error in selfTestErrors)
            {
                Console.Error.WriteLine($"module manifest probe self-test: {error}");
            }

            return 1;
        }

        var moduleRootArgument = ModuleRootArgument(args);
        var moduleRoot = moduleRootArgument is not null
            ? Path.GetFullPath(moduleRootArgument)
            : Path.GetFullPath("octaryn-basegame");
        var registration = new BasegameModuleRegistration();
        var errors = Validate(moduleRoot, registration.Manifest);
        errors.AddRange(ValidatePackageDescriptor(moduleRoot, registration.Manifest));
        if (errors.Count == 0)
        {
            var dumpPath = ArgumentValue(args, "--dump-manifest");
            if (dumpPath is not null)
            {
                WriteManifestDump(registration.Manifest, dumpPath);
            }

            return 0;
        }

        foreach (var error in errors)
        {
            Console.Error.WriteLine($"module manifest probe: {error}");
        }

        return 1;
    }

    private static string? ArgumentValue(IReadOnlyList<string> args, string name)
    {
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (args[index] == name)
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static string? ModuleRootArgument(IReadOnlyList<string> args)
    {
        for (var index = 0; index < args.Count; index++)
        {
            if (args[index].StartsWith("--", StringComparison.Ordinal))
            {
                index++;
                continue;
            }

            return args[index];
        }

        return null;
    }

    private static void WriteManifestDump(GameModuleManifest manifest, string path)
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        File.WriteAllText(fullPath, JsonSerializer.Serialize(manifest, options));
    }

    private static List<string> ValidatePackageDescriptor(string moduleRoot, GameModuleManifest manifest)
    {
        var errors = new List<string>();
        var descriptorPath = Path.Combine(moduleRoot, "Data", "Module", $"{manifest.ModuleId}.module.json");
        if (!File.Exists(descriptorPath))
        {
            errors.Add($"{manifest.ModuleId}: package descriptor missing at Data/Module/{manifest.ModuleId}.module.json");
            return errors;
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var descriptor = JsonNode.Parse(File.ReadAllText(descriptorPath));
        var generated = JsonSerializer.SerializeToNode(manifest, options);
        if (!JsonNode.DeepEquals(descriptor, generated))
        {
            errors.Add($"{manifest.ModuleId}: package descriptor does not match code manifest");
        }

        return errors;
    }

    private static List<string> RunSelfTests()
    {
        var errors = new List<string>();
        ExpectInvalid(
            errors,
            "content path owner",
            ValidManifest(contentPath: "Assets/x.json"),
            "module.content.path.owner.invalid");
        ExpectInvalid(
            errors,
            "asset path owner",
            ValidManifest(assetPath: "Data/x.txt"),
            "module.asset.path.owner.invalid");
        ExpectInvalid(
            errors,
            "path traversal",
            ValidManifest(contentPath: "Data/../x.json"),
            "module.content.path.invalid");
        ExpectInvalid(
            errors,
            "absolute path",
            ValidManifest(contentPath: "/Data/x.json"),
            "module.content.path.invalid");
        ExpectInvalid(
            errors,
            "colon path",
            ValidManifest(contentPath: "Data/C:/x.json"),
            "module.content.path.invalid");
        ExpectInvalid(
            errors,
            "duplicate declaration ID",
            ValidManifest(assetId: "octaryn.test.content"),
            "module.declaration.id.duplicate");
        ExpectInvalid(
            errors,
            "non-owned content ID",
            ValidManifest(contentId: "other.content"),
            "module.content.id.owner.invalid");
        ExpectInvalid(
            errors,
            "non-owned write resource",
            ValidManifest(systems:
            [
                ScheduledSystem("octaryn.test.tick", writes: [new ScheduledResourceAccess("other.state", ScheduledAccessMode.Write)])
            ]),
            "module.schedule.write.resource_id.owner.invalid");
        ExpectInvalid(
            errors,
            "non-owned read resource",
            ValidManifest(systems:
            [
                ScheduledSystem("octaryn.test.tick", reads: [new ScheduledResourceAccess("other.state", ScheduledAccessMode.Read)])
            ]),
            "module.schedule.read.resource_id.owner.invalid");
        ExpectInvalid(
            errors,
            "unexposed host write resource",
            ValidManifest(systems:
            [
                ScheduledSystem("octaryn.test.tick", writes: [new ScheduledResourceAccess("host.private", ScheduledAccessMode.Write)])
            ]),
            "module.schedule.write.host_resource.invalid");
        ExpectInvalid(
            errors,
            "commands requested without scheduled write",
            ValidManifest(systems:
            [
                ScheduledSystem("octaryn.test.tick", includeCommandWrite: false)
            ]),
            "module.schedule.commands.write.required");
        ExpectInvalid(
            errors,
            "commands requested without block edit capability",
            ValidManifest(requiredCapabilities:
            [
                ModuleCapabilityIds.ContentBlocks,
                ModuleCapabilityIds.ContentItems,
                ModuleCapabilityIds.GameplayRules
            ]),
            "module.capability.world_block_edits.required");
        ExpectInvalid(
            errors,
            "scheduled command write without block edit capability",
            ValidManifest(
                requiredCapabilities:
                [
                    ModuleCapabilityIds.ContentBlocks,
                    ModuleCapabilityIds.ContentItems,
                    ModuleCapabilityIds.GameplayRules
                ],
                requestedHostApis: [HostApiIds.Frame],
                systems:
                [
                    ScheduledSystem("octaryn.test.tick")
                ]),
            "module.capability.world_block_edits.required");
        ExpectInvalid(
            errors,
            "unexposed host read resource",
            ValidManifest(systems:
            [
                ScheduledSystem("octaryn.test.tick", reads: [new ScheduledResourceAccess("host.private", ScheduledAccessMode.Read)])
            ]),
            "module.schedule.read.host_resource.invalid");
        ExpectInvalid(
            errors,
            "read resource write mode",
            ValidManifest(systems:
            [
                ScheduledSystem(
                    "octaryn.test.tick",
                    reads: [new ScheduledResourceAccess("host.frame", ScheduledAccessMode.Write)])
            ]),
            "module.schedule.read.mode.mismatch");
        ExpectInvalid(
            errors,
            "write resource read mode",
            ValidManifest(systems:
            [
                ScheduledSystem(
                    "octaryn.test.tick",
                    writes: [new ScheduledResourceAccess("octaryn.test.state", ScheduledAccessMode.Read)])
            ]),
            "module.schedule.write.mode.mismatch");
        ExpectInvalid(
            errors,
            "invalid content kind",
            ValidManifest(contentKind: "mesh"),
            "module.content.kind.invalid");
        ExpectInvalid(
            errors,
            "invalid asset kind",
            ValidManifest(assetKind: "save"),
            "module.asset.kind.invalid");
        ExpectInvalid(
            errors,
            "invalid version range",
            ValidManifest(minimumHostApiVersion: "0.2.0", maximumHostApiVersion: "0.1.0"),
            "module.compatibility.host_api_range.invalid");
        ExpectInvalid(
            errors,
            "missing frame API for scheduled frame read",
            ValidManifest(requestedHostApis: [HostApiIds.Commands]),
            "module.schedule.frame.read.required");
        ExpectInvalid(
            errors,
            "self dependency",
            ValidManifest(systems:
            [
                ScheduledSystem("octaryn.test.tick", runsAfter: ["octaryn.test.tick"])
            ]),
            "module.schedule.runs_after.self");
        ExpectInvalid(
            errors,
            "unknown dependency",
            ValidManifest(systems:
            [
                ScheduledSystem("octaryn.test.tick", runsAfter: ["octaryn.test.missing"])
            ]),
            "module.schedule.runs_after.unknown");
        ExpectInvalid(
            errors,
            "schedule graph cycle",
            ValidManifest(systems:
            [
                ScheduledSystem("octaryn.test.first", runsAfter: ["octaryn.test.second"]),
                ScheduledSystem("octaryn.test.second", runsAfter: ["octaryn.test.first"])
            ]),
            "module.schedule.graph.cycle");
        ExpectInvalid(
            errors,
            "read write resource overlap",
            ValidManifest(systems:
            [
                ScheduledSystem("octaryn.test.tick", reads: [new ScheduledResourceAccess("octaryn.test.state", ScheduledAccessMode.Read)])
            ]),
            "module.schedule.resource.read_write_overlap");
        ExpectInvalid(
            errors,
            "unordered cross-system write conflict",
            ValidManifest(systems:
            [
                ScheduledSystem("octaryn.test.first"),
                ScheduledSystem("octaryn.test.second")
            ]),
            "module.schedule.resource.conflict_unordered");
        ExpectFileGraphInvalid(
            errors,
            "missing declared content file",
            ValidManifest(),
            "declared file missing");
        ExpectFileGraphInvalid(
            errors,
            "empty declared content file",
            ValidManifest(),
            "declared file is empty",
            root => WriteFile(root, "Data/Rules/octaryn.test.rule.json", string.Empty));
        ExpectFileGraphInvalid(
            errors,
            "undeclared data file",
            ValidManifest(),
            "undeclared content file",
            root => WriteValidManifestFiles(root),
            root => WriteFile(root, "Data/Rules/octaryn.test.extra.json", "{}"));
        ExpectFileGraphInvalid(
            errors,
            "undeclared asset file",
            ValidManifest(),
            "undeclared asset file",
            root => WriteValidManifestFiles(root),
            root => WriteFile(root, "Assets/Textures/octaryn.test.extra.txt", "asset"));
        ExpectFileGraphInvalid(
            errors,
            "undeclared shader file",
            ValidManifest(),
            "undeclared shader file",
            root => WriteValidManifestFiles(root),
            root => WriteFile(root, "Shaders/octaryn.test.shader.glsl", "void main() {}"));

        return errors;
    }

    private static void ExpectInvalid(
        List<string> errors,
        string name,
        GameModuleManifest manifest,
        string expectedCode)
    {
        var report = GameModuleValidator.Validate(manifest);
        if (!report.Issues.Any(issue => issue.Code == expectedCode))
        {
            errors.Add($"{name}: expected {expectedCode}, got {string.Join(", ", report.Issues.Select(issue => issue.Code))}");
        }
    }

    private static GameModuleManifest ValidManifest(
        string contentId = "octaryn.test.content",
        string contentPath = "Data/Rules/octaryn.test.rule.json",
        string contentKind = "rule",
        string assetId = "octaryn.test.asset",
        string assetKind = "texture",
        string assetPath = "Assets/Textures/octaryn.test.texture.txt",
        string minimumHostApiVersion = "0.1.0",
        string maximumHostApiVersion = "0.1.0",
        IReadOnlyList<string>? requiredCapabilities = null,
        IReadOnlyList<string>? requestedHostApis = null,
        IReadOnlyList<ScheduledSystemDeclaration>? systems = null)
    {
        var hostApis = requestedHostApis ??
        [
            HostApiIds.Commands,
            HostApiIds.Frame
        ];

        return new GameModuleManifest(
            ModuleId: "octaryn.test",
            DisplayName: "Octaryn Test Module",
            Version: "0.1.0",
            OctarynApiVersion: "0.1.0",
            RequiredCapabilities: requiredCapabilities ??
            [
                ModuleCapabilityIds.ContentBlocks,
                ModuleCapabilityIds.ContentItems,
                ModuleCapabilityIds.GameplayRules,
                ModuleCapabilityIds.WorldBlockEdits
            ],
            RequestedHostApis: hostApis,
            RequestedRuntimePackages: [],
            RequestedBuildPackages: [],
            RequestedFrameworkApiGroups:
            [
                FrameworkApiGroupIds.BclPrimitives
            ],
            ModuleDependencies: [],
            ContentDeclarations:
            [
                new GameModuleContentDeclaration(contentId, contentKind, contentPath)
            ],
            AssetDeclarations:
            [
                new GameModuleAssetDeclaration(assetId, assetKind, assetPath)
            ],
            Schedule: new GameModuleScheduleDeclaration(systems ?? [ScheduledSystem("octaryn.test.tick", includeCommandWrite: hostApis.Contains(HostApiIds.Commands, StringComparer.Ordinal))]),
            Compatibility: new GameModuleCompatibility(
                MinimumHostApiVersion: minimumHostApiVersion,
                MaximumHostApiVersion: maximumHostApiVersion,
                SaveCompatibilityId: "octaryn.test.save.v0",
                SupportsMultiplayer: false));
    }

    private static void ExpectFileGraphInvalid(
        List<string> errors,
        string name,
        GameModuleManifest manifest,
        string expectedText,
        params Action<string>[] setup)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"octaryn-module-manifest-probe-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(Path.Combine(tempRoot, "Data"));
            Directory.CreateDirectory(Path.Combine(tempRoot, "Assets"));
            Directory.CreateDirectory(Path.Combine(tempRoot, "Shaders"));
            foreach (var action in setup)
            {
                action(tempRoot);
            }

            var validationErrors = Validate(tempRoot, manifest);
            if (!validationErrors.Any(error => error.Contains(expectedText, StringComparison.Ordinal)))
            {
                errors.Add($"{name}: expected {expectedText}, got {string.Join(", ", validationErrors)}");
            }
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static void WriteValidManifestFiles(string moduleRoot)
    {
        WriteFile(moduleRoot, "Data/Rules/octaryn.test.rule.json", "{}");
        WriteFile(moduleRoot, "Assets/Textures/octaryn.test.texture.txt", "asset");
    }

    private static void WriteFile(string moduleRoot, string relativePath, string text)
    {
        var path = Path.Combine(moduleRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    private static ScheduledSystemDeclaration ScheduledSystem(
        string systemId,
        IReadOnlyList<ScheduledResourceAccess>? reads = null,
        IReadOnlyList<ScheduledResourceAccess>? writes = null,
        IReadOnlyList<string>? runsAfter = null,
        bool includeCommandWrite = true)
    {
        return new ScheduledSystemDeclaration(
            SystemId: systemId,
            Phase: HostWorkPhase.Gameplay,
            FrameOrTickOwner: HostScheduleIds.FrameOrTickOwner,
            Reads: reads ??
            [
                new ScheduledResourceAccess("host.frame", ScheduledAccessMode.Read)
            ],
            Writes: writes ?? DefaultWrites(includeCommandWrite),
            RunsAfter: runsAfter ?? [],
            RunsBefore: [],
            Flags: HostWorkScheduleFlags.DeterministicOrder | HostWorkScheduleFlags.RequiresTickBarrier,
            CommitBarrier: HostScheduleIds.FrameOrTickEndBarrier);
    }

    private static IReadOnlyList<ScheduledResourceAccess> DefaultWrites(bool includeCommandWrite)
    {
        var writes = new List<ScheduledResourceAccess>
        {
            new("octaryn.test.state", ScheduledAccessMode.Write)
        };
        if (includeCommandWrite)
        {
            writes.Add(new ScheduledResourceAccess(HostApiIds.Commands, ScheduledAccessMode.Write));
        }

        return writes;
    }

    private static List<string> Validate(string moduleRoot, GameModuleManifest manifest)
    {
        var errors = new List<string>();
        var report = GameModuleValidator.Validate(manifest);
        errors.AddRange(
            report.Issues
                .Where(issue => issue.Severity == ModuleValidationSeverity.Error)
                .Select(issue => issue.Message));

        var declaredContent = new HashSet<string>(StringComparer.Ordinal);
        foreach (var content in manifest.ContentDeclarations)
        {
            ValidateDeclaredFile(
                errors,
                moduleRoot,
                declaredContent,
                content.ContentId,
                content.RelativePath,
                ["Data/"]);
        }

        var declaredAssets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var asset in manifest.AssetDeclarations)
        {
            ValidateDeclaredFile(
                errors,
                moduleRoot,
                declaredAssets,
                asset.AssetId,
                asset.RelativePath,
                ["Assets/", "Shaders/"]);
        }

        ValidateUndeclaredFiles(errors, moduleRoot, "Data", declaredContent, "content");
        ValidateUndeclaredFiles(errors, moduleRoot, "Assets", declaredAssets, "asset");
        ValidateUndeclaredFiles(errors, moduleRoot, "Shaders", declaredAssets, "shader");
        return errors;
    }

    private static void ValidateDeclaredFile(
        List<string> errors,
        string moduleRoot,
        HashSet<string> declaredPaths,
        string declarationId,
        string relativePath,
        IReadOnlyList<string> allowedPrefixes)
    {
        if (Path.IsPathRooted(relativePath) ||
            relativePath.Contains("..", StringComparison.Ordinal) ||
            relativePath.Contains(':', StringComparison.Ordinal))
        {
            errors.Add($"{declarationId}: unsafe relative path {relativePath}");
            return;
        }

        if (!allowedPrefixes.Any(prefix => relativePath.StartsWith(prefix, StringComparison.Ordinal)))
        {
            errors.Add($"{declarationId}: path {relativePath} is outside allowed module roots.");
            return;
        }

        declaredPaths.Add(relativePath);
        var moduleFullPath = Path.GetFullPath(moduleRoot);
        var path = Path.GetFullPath(Path.Combine(moduleFullPath, relativePath));
        if (!path.StartsWith(moduleFullPath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            errors.Add($"{declarationId}: path escapes module root {relativePath}");
            return;
        }

        if (!File.Exists(path))
        {
            errors.Add($"{declarationId}: declared file missing at {relativePath}");
            return;
        }

        if (new FileInfo(path).Length == 0)
        {
            errors.Add($"{declarationId}: declared file is empty at {relativePath}");
        }
    }

    private static void ValidateUndeclaredFiles(
        List<string> errors,
        string moduleRoot,
        string directoryName,
        HashSet<string> declaredPaths,
        string fileKind)
    {
        var directory = Path.Combine(moduleRoot, directoryName);
        if (!Directory.Exists(directory))
        {
            errors.Add($"{moduleRoot}: missing {directoryName}/ directory");
            return;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(file) == ".gitkeep")
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(moduleRoot, file).Replace('\\', '/');
            if (fileKind == "content" &&
                relativePath.StartsWith("Data/Module/", StringComparison.Ordinal) &&
                relativePath.EndsWith(".module.json", StringComparison.Ordinal))
            {
                continue;
            }

            if (!declaredPaths.Contains(relativePath))
            {
                errors.Add($"{relativePath}: undeclared {fileKind} file");
            }
        }
    }
}
