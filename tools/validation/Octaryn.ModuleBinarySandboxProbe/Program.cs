using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using Octaryn.Shared.FrameworkAllowlist;

return ModuleBinarySandboxProbe.Run(args);

internal static class ModuleBinarySandboxProbe
{
    private static readonly IReadOnlyDictionary<string, string> DeniedNamespaces = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["System.Console"] = FrameworkApiGroupIds.BclConsole,
        ["System.Environment"] = FrameworkApiGroupIds.BclEnvironment,
        ["System.IO"] = FrameworkApiGroupIds.BclFilesystem,
        ["System.Linq.Expressions"] = FrameworkApiGroupIds.BclRuntimeCodeGeneration,
        ["System.Net"] = FrameworkApiGroupIds.BclNetworking,
        ["System.Reflection.Emit"] = FrameworkApiGroupIds.BclRuntimeCodeGeneration,
        ["System.Runtime.InteropServices"] = FrameworkApiGroupIds.BclNativeInterop,
        ["System.Runtime.Loader"] = FrameworkApiGroupIds.BclRuntimeCodeGeneration,
        ["System.Threading"] = FrameworkApiGroupIds.BclThreading,
        ["System.Threading.Tasks"] = FrameworkApiGroupIds.BclThreading,
        ["System.Timers"] = FrameworkApiGroupIds.BclThreading
    };

    private static readonly IReadOnlyDictionary<string, string> DeniedTypeNames = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Activator"] = FrameworkApiGroupIds.BclReflection,
        ["AppDomain"] = FrameworkApiGroupIds.BclReflection,
        ["Console"] = FrameworkApiGroupIds.BclConsole,
        ["Delegate"] = FrameworkApiGroupIds.BclReflection,
        ["Directory"] = FrameworkApiGroupIds.BclFilesystem,
        ["DirectoryInfo"] = FrameworkApiGroupIds.BclFilesystem,
        ["Environment"] = FrameworkApiGroupIds.BclEnvironment,
        ["File"] = FrameworkApiGroupIds.BclFilesystem,
        ["FileInfo"] = FrameworkApiGroupIds.BclFilesystem,
        ["FileStream"] = FrameworkApiGroupIds.BclFilesystem,
        ["HttpClient"] = FrameworkApiGroupIds.BclNetworking,
        ["Monitor"] = FrameworkApiGroupIds.BclThreading,
        ["Parallel"] = FrameworkApiGroupIds.BclThreading,
        ["Path"] = FrameworkApiGroupIds.BclFilesystem,
        ["Process"] = FrameworkApiGroupIds.BclProcess,
        ["ProcessStartInfo"] = FrameworkApiGroupIds.BclProcess,
        ["SemaphoreSlim"] = FrameworkApiGroupIds.BclThreading,
        ["Socket"] = FrameworkApiGroupIds.BclNetworking,
        ["StreamReader"] = FrameworkApiGroupIds.BclFilesystem,
        ["StreamWriter"] = FrameworkApiGroupIds.BclFilesystem,
        ["Task"] = FrameworkApiGroupIds.BclThreading,
        ["Thread"] = FrameworkApiGroupIds.BclThreading,
        ["ThreadPool"] = FrameworkApiGroupIds.BclThreading,
        ["Timer"] = FrameworkApiGroupIds.BclThreading,
        ["UnmanagedCallersOnlyAttribute"] = FrameworkApiGroupIds.BclNativeInterop
    };

    private static readonly IReadOnlySet<string> DeniedFullTypeNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "System.Reflection.Assembly",
        "System.Reflection.ConstructorInfo",
        "System.Reflection.EventInfo",
        "System.Reflection.FieldInfo",
        "System.Reflection.MemberInfo",
        "System.Reflection.MethodBase",
        "System.Reflection.MethodInfo",
        "System.Reflection.PropertyInfo",
        "System.Reflection.TypeInfo",
        "System.Runtime.InteropServices.DllImportAttribute",
        "System.Runtime.InteropServices.LibraryImportAttribute",
        "System.Runtime.InteropServices.Marshal",
        "System.Runtime.Loader.AssemblyLoadContext",
        "System.Diagnostics.Process",
        "System.Diagnostics.ProcessStartInfo"
    };

    public static int Run(IReadOnlyList<string> args)
    {
        var selfTestErrors = RunSelfTests();
        if (selfTestErrors.Count > 0)
        {
            foreach (var error in selfTestErrors)
            {
                Console.Error.WriteLine($"module binary sandbox probe self-test: {error}");
            }

            return 1;
        }

        var assemblyPath = ParseAssemblyPath(args);
        if (assemblyPath is null)
        {
            Console.Error.WriteLine("module binary sandbox probe: --assembly <path> is required");
            return 1;
        }

        var assetsPath = ParseOption(args, "--assets-file");
        var policyPath = ParseOption(args, "--policy-file");
        var errors = ValidateAssembly(assemblyPath, assetsPath, policyPath);
        if (errors.Count == 0)
        {
            return 0;
        }

        foreach (var error in errors)
        {
            Console.Error.WriteLine($"module binary sandbox probe: {error}");
        }

        return 1;
    }

    private static string? ParseAssemblyPath(IReadOnlyList<string> args)
    {
        for (var index = 0; index < args.Count; index++)
        {
            if (args[index] == "--assembly" && index + 1 < args.Count)
            {
                return Path.GetFullPath(args[index + 1]);
            }
        }

        return args.Count > 0 ? Path.GetFullPath(args[0]) : null;
    }

    private static string? ParseOption(IReadOnlyList<string> args, string name)
    {
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (args[index] == name)
            {
                return Path.GetFullPath(args[index + 1]);
            }
        }

        return null;
    }

    private static List<string> RunSelfTests()
    {
        var errors = new List<string>();
        ExpectDenied(errors, "filesystem", "System.IO", "File", null, FrameworkApiGroupIds.BclFilesystem);
        ExpectDenied(errors, "networking", "System.Net.Http", "HttpClient", null, FrameworkApiGroupIds.BclNetworking);
        ExpectDenied(errors, "process", "System.Diagnostics", "Process", "Start", "System.Diagnostics.Process");
        ExpectDenied(errors, "reflection type", "System", "Type", "GetMethods", FrameworkApiGroupIds.BclReflection);
        ExpectDenied(errors, "reflection assembly", "System.Reflection", "Assembly", "Load", "System.Reflection.Assembly");
        ExpectDenied(errors, "runtime loader", "System.Runtime.Loader", "AssemblyLoadContext", null, "System.Runtime.Loader.AssemblyLoadContext");
        ExpectDenied(errors, "native marshal", "System.Runtime.InteropServices", "Marshal", null, "System.Runtime.InteropServices.Marshal");
        ExpectDenied(errors, "threading task", "System.Threading.Tasks", "Task", "Run", FrameworkApiGroupIds.BclThreading);
        ExpectDenied(errors, "threading parallel", "System.Threading.Tasks", "Parallel", "For", FrameworkApiGroupIds.BclThreading);
        ExpectAllowed(errors, "compiler attribute", "System.Runtime.CompilerServices", "NullableContextAttribute", ".ctor");
        ValidateAllowedAssemblyReferencePolicySelfTests(errors);
        return errors;
    }

    private static void ValidateAllowedAssemblyReferencePolicySelfTests(List<string> errors)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"octaryn-binary-sandbox-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempRoot);
            var assetsPath = Path.Combine(tempRoot, "project.assets.json");
            var policyPath = Path.Combine(tempRoot, "module-packages.json");
            File.WriteAllText(policyPath, """
                {
                  "runtimeDirect": {"Arch": "2.1.0"},
                  "runtimeTransitive": {"Good.Transitive": "1.0.0"}
                }
                """);
            var validAssets = """
                {
                  "projectFileDependencyGroups": {
                    "net10.0": [
                      "Arch >= 2.1.0",
                      "Octaryn.Shared >= 1.0.0"
                    ]
                  },
                  "targets": {
                    "net10.0": {
                      "Arch/2.1.0": {
                        "dependencies": {
                          "Good.Transitive": "1.0.0",
                          "Bad.Transitive": "1.0.0"
                        },
                        "compile": {"lib/net8.0/Arch.dll": {}},
                        "runtime": {"lib/net8.0/Arch.dll": {}}
                      },
                      "Good.Transitive/1.0.0": {
                        "compile": {"lib/net8.0/Good.Transitive.dll": {}},
                        "runtime": {"lib/net8.0/Good.Transitive.dll": {}}
                      },
                      "Bad.Transitive/1.0.0": {
                        "compile": {"lib/net8.0/Bad.Transitive.dll": {}},
                        "runtime": {"lib/net8.0/Bad.Transitive.dll": {}}
                      },
                      "Octaryn.Shared/1.0.0": {
                        "type": "project",
                        "compile": {"bin/placeholder/Octaryn.Shared.dll": {}},
                        "runtime": {"bin/placeholder/Octaryn.Shared.dll": {}}
                      }
                    }
                  },
                  "libraries": {
                    "Arch/2.1.0": {"type": "package", "path": "arch/2.1.0"},
                    "Good.Transitive/1.0.0": {"type": "package", "path": "good.transitive/1.0.0"},
                    "Bad.Transitive/1.0.0": {"type": "package", "path": "bad.transitive/1.0.0"},
                    "Octaryn.Shared/1.0.0": {"type": "project", "path": "../octaryn-shared/Octaryn.Shared.csproj"}
                  }
                }
                """;
            File.WriteAllText(assetsPath, validAssets);

            var allowed = LoadAllowedAssemblyReferences(assetsPath, policyPath, errors);
            ExpectAllowedAssembly(errors, allowed, "Arch");
            ExpectAllowedAssembly(errors, allowed, "Good.Transitive");
            ExpectAllowedAssembly(errors, allowed, "Octaryn.Shared");
            ExpectDeniedAssembly(errors, allowed, "Bad.Transitive");

            File.WriteAllText(assetsPath, File.ReadAllText(assetsPath).Replace(
                "../octaryn-shared/Octaryn.Shared.csproj",
                "../spoof/Octaryn.Shared.csproj"));
            var spoofedSharedAllowed = LoadAllowedAssemblyReferences(assetsPath, policyPath, errors);
            ExpectDeniedAssembly(errors, spoofedSharedAllowed, "Octaryn.Shared");

            File.WriteAllText(assetsPath, validAssets.Replace(
                "\"Octaryn.Shared/1.0.0\": {\"type\": \"project\", \"path\": \"../octaryn-shared/Octaryn.Shared.csproj\"}",
                "\"Octaryn.Shared/1.0.0\": {\"type\": \"package\", \"path\": \"octaryn.shared/1.0.0\"}"));
            var packageSharedAllowed = LoadAllowedAssemblyReferences(assetsPath, policyPath, errors);
            ExpectDeniedAssembly(errors, packageSharedAllowed, "Octaryn.Shared");

            File.WriteAllText(assetsPath, validAssets.Replace(
                "\"Octaryn.Shared/1.0.0\": {\"type\": \"project\", \"path\": \"../octaryn-shared/Octaryn.Shared.csproj\"}",
                "\"Octaryn.Shared/1.0.0\": {\"type\": \"project\"}"));
            var missingPathSharedAllowed = LoadAllowedAssemblyReferences(assetsPath, policyPath, errors);
            ExpectDeniedAssembly(errors, missingPathSharedAllowed, "Octaryn.Shared");
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static void ExpectAllowedAssembly(List<string> errors, IReadOnlySet<string> allowed, string assemblyName)
    {
        if (!allowed.Contains(assemblyName))
        {
            errors.Add($"assembly policy: expected allowed assembly {assemblyName}");
        }
    }

    private static void ExpectDeniedAssembly(List<string> errors, IReadOnlySet<string> allowed, string assemblyName)
    {
        if (allowed.Contains(assemblyName))
        {
            errors.Add($"assembly policy: expected denied assembly {assemblyName}");
        }
    }

    private static void ExpectDenied(
        List<string> errors,
        string name,
        string namespaceName,
        string typeName,
        string? memberName,
        string expectedText)
    {
        var classification = new List<string>();
        ValidateTypeName(classification, "self-test.dll", namespaceName, typeName, memberName);
        if (!classification.Any(error => error.Contains(expectedText, StringComparison.Ordinal)))
        {
            errors.Add($"{name}: expected denied metadata containing {expectedText}, got {string.Join(", ", classification)}");
        }
    }

    private static void ExpectAllowed(
        List<string> errors,
        string name,
        string namespaceName,
        string typeName,
        string? memberName)
    {
        var classification = new List<string>();
        ValidateTypeName(classification, "self-test.dll", namespaceName, typeName, memberName);
        if (classification.Count > 0)
        {
            errors.Add($"{name}: expected allowed metadata, got {string.Join(", ", classification)}");
        }
    }

    private static List<string> ValidateAssembly(string assemblyPath, string? assetsPath, string? policyPath)
    {
        var errors = new List<string>();
        if (!File.Exists(assemblyPath))
        {
            errors.Add($"{assemblyPath}: assembly does not exist");
            return errors;
        }

        var allowedAssemblies = assetsPath is null
            ? null
            : LoadAllowedAssemblyReferences(assetsPath, policyPath, errors);

        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        if (!peReader.HasMetadata)
        {
            errors.Add($"{assemblyPath}: assembly has no .NET metadata");
            return errors;
        }

        var metadata = peReader.GetMetadataReader();
        ValidateAssemblyReferences(errors, assemblyPath, metadata, allowedAssemblies);
        ValidateTypeReferences(errors, assemblyPath, metadata);
        ValidateMemberReferences(errors, assemblyPath, metadata);
        ValidatePinvokeMaps(errors, assemblyPath, metadata);
        return errors;
    }

    private static void ValidateAssemblyReferences(
        List<string> errors,
        string assemblyPath,
        MetadataReader metadata,
        IReadOnlySet<string>? allowedAssemblies)
    {
        foreach (var handle in metadata.AssemblyReferences)
        {
            var reference = metadata.GetAssemblyReference(handle);
            var name = metadata.GetString(reference.Name);
            if (name is "System.IO" or "System.Net.Http" or "System.Diagnostics.Process" or "System.Reflection.Emit")
            {
                errors.Add($"{assemblyPath}: denied assembly reference {name}");
                continue;
            }

            if (IsFrameworkAssembly(name) && !IsTrustedFrameworkAssembly(metadata, reference))
            {
                errors.Add($"{assemblyPath}: untrusted framework assembly reference {name}");
                continue;
            }

            if (IsFrameworkAssembly(name) && HasLocalAssemblyFile(assemblyPath, name))
            {
                errors.Add($"{assemblyPath}: local framework assembly spoof is not allowed: {name}.dll");
                continue;
            }

            if (allowedAssemblies is not null &&
                !IsFrameworkAssembly(name) &&
                !allowedAssemblies.Contains(name))
            {
                errors.Add($"{assemblyPath}: unexpected assembly reference {name}");
            }
        }
    }

    private static IReadOnlySet<string> LoadAllowedAssemblyReferences(
        string assetsPath,
        string? policyPath,
        List<string> errors)
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal);

        if (!File.Exists(assetsPath))
        {
            errors.Add($"{assetsPath}: project assets file does not exist");
            return allowed;
        }

        var policy = LoadRuntimePackagePolicy(assetsPath, policyPath, errors);
        if (policy.Direct.Count == 0)
        {
            return allowed;
        }

        using var assetsStream = File.OpenRead(assetsPath);
        using var assetsDocument = JsonDocument.Parse(assetsStream);
        var directProjectPackages = LoadDirectProjectPackages(assetsDocument.RootElement);
        if (!assetsDocument.RootElement.TryGetProperty("targets", out var targets) ||
            targets.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"{assetsPath}: project assets file has no targets object");
            return allowed;
        }

        var packageLibraries = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        var packageDependencies = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var target in targets.EnumerateObject())
        {
            foreach (var library in target.Value.EnumerateObject())
            {
                var packageId = PackageId(library.Name);
                packageLibraries[packageId] = library.Value;
                packageDependencies[packageId] = DependencyIds(library.Value);
                if (packageId == "Octaryn.Shared" && IsVerifiedSharedProject(assetsDocument.RootElement, library.Name))
                {
                    AddAssetAssemblies(allowed, library.Value, "compile");
                    AddAssetAssemblies(allowed, library.Value, "runtime");
                }
            }
        }

        var runtimeClosure = RuntimeClosureFrom(directProjectPackages.Intersect(policy.Direct, StringComparer.Ordinal), packageDependencies, policy);
        foreach (var packageId in runtimeClosure)
        {
            if (packageLibraries.TryGetValue(packageId, out var library))
            {
                AddAssetAssemblies(allowed, library, "compile");
                AddAssetAssemblies(allowed, library, "runtime");
            }
        }

        return allowed;
    }

    private static IReadOnlyList<string> DependencyIds(JsonElement library)
    {
        if (!library.TryGetProperty("dependencies", out var dependencies) ||
            dependencies.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        return dependencies
            .EnumerateObject()
            .Select(dependency => dependency.Name)
            .ToArray();
    }

    private static IReadOnlySet<string> RuntimeClosureFrom(
        IEnumerable<string> roots,
        IReadOnlyDictionary<string, IReadOnlyList<string>> dependencies,
        RuntimePackagePolicy policy)
    {
        var allowedPackages = policy.Direct
            .Concat(policy.Transitive)
            .ToHashSet(StringComparer.Ordinal);
        var closure = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<string>(roots);
        while (pending.Count > 0)
        {
            var packageId = pending.Dequeue();
            if (!allowedPackages.Contains(packageId) || !closure.Add(packageId))
            {
                continue;
            }

            if (!dependencies.TryGetValue(packageId, out var packageDependencies))
            {
                continue;
            }

            foreach (var dependency in packageDependencies)
            {
                pending.Enqueue(dependency);
            }
        }

        return closure;
    }

    private static bool IsVerifiedSharedProject(JsonElement root, string targetKey)
    {
        if (PackageId(targetKey) != "Octaryn.Shared" ||
            !root.TryGetProperty("libraries", out var libraries) ||
            libraries.ValueKind != JsonValueKind.Object ||
            !libraries.TryGetProperty(targetKey, out var library) ||
            library.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!library.TryGetProperty("type", out var type) ||
            type.GetString() != "project" ||
            !library.TryGetProperty("path", out var path))
        {
            return false;
        }

        var normalizedPath = path.GetString()?.Replace('\\', '/');
        return normalizedPath == "../octaryn-shared/Octaryn.Shared.csproj";
    }

    private static RuntimePackagePolicy LoadRuntimePackagePolicy(
        string assetsPath,
        string? policyPath,
        List<string> errors)
    {
        var resolvedPolicyPath = policyPath ?? FindDefaultPolicyPath(assetsPath);
        if (resolvedPolicyPath is null || !File.Exists(resolvedPolicyPath))
        {
            errors.Add("module package policy file does not exist");
            return new RuntimePackagePolicy(
                new HashSet<string>(StringComparer.Ordinal),
                new HashSet<string>(StringComparer.Ordinal));
        }

        using var stream = File.OpenRead(resolvedPolicyPath);
        using var document = JsonDocument.Parse(stream);
        return new RuntimePackagePolicy(
            LoadPolicyPackageSection(document.RootElement, resolvedPolicyPath, "runtimeDirect", errors),
            LoadPolicyPackageSection(document.RootElement, resolvedPolicyPath, "runtimeTransitive", errors));
    }

    private static IReadOnlySet<string> LoadPolicyPackageSection(
        JsonElement root,
        string policyPath,
        string section,
        List<string> errors)
    {
        if (!root.TryGetProperty(section, out var packages) ||
            packages.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"{policyPath}: package policy has no {section} object");
            return new HashSet<string>(StringComparer.Ordinal);
        }

        return packages
            .EnumerateObject()
            .Select(package => package.Name)
            .ToHashSet(StringComparer.Ordinal);
    }

    private readonly record struct RuntimePackagePolicy(
        IReadOnlySet<string> Direct,
        IReadOnlySet<string> Transitive);

    private static string? FindDefaultPolicyPath(string assetsPath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(assetsPath));
        while (directory is not null)
        {
            var candidate = Path.Combine(directory, "tools", "package-policy", "module-packages.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        return null;
    }

    private static IReadOnlySet<string> LoadDirectProjectPackages(JsonElement root)
    {
        var packages = new HashSet<string>(StringComparer.Ordinal);
        if (!root.TryGetProperty("projectFileDependencyGroups", out var groups) ||
            groups.ValueKind != JsonValueKind.Object)
        {
            return packages;
        }

        foreach (var group in groups.EnumerateObject())
        {
            if (group.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var dependency in group.Value.EnumerateArray())
            {
                var text = dependency.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    packages.Add(text.Split(' ', 2)[0]);
                }
            }
        }

        return packages;
    }

    private static string PackageId(string assetKey)
    {
        var slashIndex = assetKey.LastIndexOf('/');
        return slashIndex < 0 ? assetKey : assetKey[..slashIndex];
    }

    private static void AddAssetAssemblies(
        HashSet<string> allowed,
        JsonElement library,
        string assetKind)
    {
        if (!library.TryGetProperty(assetKind, out var assets) ||
            assets.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var asset in assets.EnumerateObject())
        {
            if (asset.Name.EndsWith("/_._", StringComparison.Ordinal))
            {
                continue;
            }

            if (Path.GetExtension(asset.Name) == ".dll")
            {
                allowed.Add(Path.GetFileNameWithoutExtension(asset.Name));
            }
        }
    }

    private static bool IsFrameworkAssembly(string assemblyName)
    {
        return assemblyName == "netstandard" ||
            assemblyName == "System" ||
            assemblyName.StartsWith("System.", StringComparison.Ordinal) ||
            assemblyName == "Microsoft.CSharp";
    }

    private static bool IsTrustedFrameworkAssembly(MetadataReader metadata, AssemblyReference reference)
    {
        var name = metadata.GetString(reference.Name);
        if (!IsFrameworkAssembly(name))
        {
            return false;
        }

        var token = metadata.GetBlobBytes(reference.PublicKeyOrToken);
        var tokenText = Convert.ToHexString(token).ToLowerInvariant();
        return tokenText is "b03f5f7f11d50a3a" or "cc7b13ffcd2ddd51";
    }

    private static bool HasLocalAssemblyFile(string assemblyPath, string assemblyName)
    {
        var assemblyDirectory = Path.GetDirectoryName(Path.GetFullPath(assemblyPath));
        return assemblyDirectory is not null &&
            File.Exists(Path.Combine(assemblyDirectory, $"{assemblyName}.dll"));
    }

    private static void ValidateTypeReferences(
        List<string> errors,
        string assemblyPath,
        MetadataReader metadata)
    {
        foreach (var handle in metadata.TypeReferences)
        {
            var type = metadata.GetTypeReference(handle);
            ValidateTypeName(errors, assemblyPath, metadata.GetString(type.Namespace), metadata.GetString(type.Name), null);
        }
    }

    private static void ValidateMemberReferences(
        List<string> errors,
        string assemblyPath,
        MetadataReader metadata)
    {
        foreach (var handle in metadata.MemberReferences)
        {
            var member = metadata.GetMemberReference(handle);
            if (member.Parent.Kind != HandleKind.TypeReference)
            {
                continue;
            }

            var type = metadata.GetTypeReference((TypeReferenceHandle)member.Parent);
            ValidateTypeName(
                errors,
                assemblyPath,
                metadata.GetString(type.Namespace),
                metadata.GetString(type.Name),
                metadata.GetString(member.Name));
        }
    }

    private static void ValidatePinvokeMaps(
        List<string> errors,
        string assemblyPath,
        MetadataReader metadata)
    {
        foreach (var handle in metadata.MethodDefinitions)
        {
            var method = metadata.GetMethodDefinition(handle);
            if ((method.Attributes & MethodAttributes.PinvokeImpl) == 0)
            {
                continue;
            }

            errors.Add($"{assemblyPath}: denied framework API group {FrameworkApiGroupIds.BclNativeInterop}: P/Invoke method metadata");
        }
    }

    private static void ValidateTypeName(
        List<string> errors,
        string assemblyPath,
        string namespaceName,
        string typeName,
        string? memberName)
    {
        if (memberName == ".ctor" && typeName.EndsWith("Attribute", StringComparison.Ordinal))
        {
            return;
        }

        var fullTypeName = string.IsNullOrEmpty(namespaceName)
            ? typeName
            : $"{namespaceName}.{typeName}";
        if (DeniedFullTypeNames.Contains(fullTypeName))
        {
            errors.Add($"{assemblyPath}: denied framework API: {fullTypeName}");
            return;
        }

        if (fullTypeName == "System.Type" && memberName == "GetTypeFromHandle")
        {
            return;
        }

        if (fullTypeName == "System.Type" && memberName is not null)
        {
            errors.Add($"{assemblyPath}: denied framework API group {FrameworkApiGroupIds.BclReflection}: {fullTypeName}.{memberName}");
            return;
        }

        if (namespaceName == "System.Diagnostics" &&
            !typeName.StartsWith("Debugger", StringComparison.Ordinal) &&
            typeName != "DebuggableAttribute")
        {
            errors.Add($"{assemblyPath}: denied framework API group {FrameworkApiGroupIds.BclProcess}: {fullTypeName}");
            return;
        }

        if (namespaceName == "System.Reflection" && memberName is not null)
        {
            errors.Add($"{assemblyPath}: denied framework API group {FrameworkApiGroupIds.BclReflection}: {fullTypeName}");
            return;
        }

        if (namespaceName == "System.Runtime.InteropServices" &&
            typeName is not ("InAttribute" or "OutAttribute" or "OptionalAttribute"))
        {
            errors.Add($"{assemblyPath}: denied framework API group {FrameworkApiGroupIds.BclNativeInterop}: {fullTypeName}");
            return;
        }

        if (namespaceName == "System.Runtime.InteropServices")
        {
            return;
        }

        if (DeniedTypeNames.TryGetValue(typeName, out var typeGroup))
        {
            errors.Add($"{assemblyPath}: denied framework API group {typeGroup}: {fullTypeName}");
            return;
        }

        foreach (var (deniedNamespace, group) in DeniedNamespaces)
        {
            if (namespaceName == deniedNamespace ||
                namespaceName.StartsWith(deniedNamespace + ".", StringComparison.Ordinal))
            {
                errors.Add($"{assemblyPath}: denied framework API group {group}: {fullTypeName}");
                return;
            }
        }
    }
}
