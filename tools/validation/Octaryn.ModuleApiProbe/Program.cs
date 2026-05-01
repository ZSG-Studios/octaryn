using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Octaryn.Shared.FrameworkAllowlist;
using Octaryn.Shared.GameModules;
using Octaryn.Shared.ModuleSandbox;
using System.Text.Json;

return ModuleApiProbe.Run(args);

internal static class ModuleApiProbe
{
    private static readonly IReadOnlyDictionary<string, string> DeniedNamespaces = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["System.Console"] = FrameworkApiGroupIds.BclConsole,
        ["System.Activator"] = FrameworkApiGroupIds.BclReflection,
        ["System.AppDomain"] = FrameworkApiGroupIds.BclReflection,
        ["System.Attribute"] = FrameworkApiGroupIds.BclReflection,
        ["System.Delegate"] = FrameworkApiGroupIds.BclReflection,
        ["System.Diagnostics"] = FrameworkApiGroupIds.BclProcess,
        ["System.Environment"] = FrameworkApiGroupIds.BclEnvironment,
        ["System.IO"] = FrameworkApiGroupIds.BclFilesystem,
        ["System.Linq.Expressions"] = FrameworkApiGroupIds.BclRuntimeCodeGeneration,
        ["System.Net"] = FrameworkApiGroupIds.BclNetworking,
        ["System.Reflection"] = FrameworkApiGroupIds.BclReflection,
        ["System.Reflection.Emit"] = FrameworkApiGroupIds.BclRuntimeCodeGeneration,
        ["System.Runtime.InteropServices"] = FrameworkApiGroupIds.BclNativeInterop,
        ["System.Runtime.Loader"] = FrameworkApiGroupIds.BclRuntimeCodeGeneration,
        ["System.ThreadStaticAttribute"] = FrameworkApiGroupIds.BclThreading,
        ["System.Type"] = FrameworkApiGroupIds.BclReflection,
        ["System.Threading"] = FrameworkApiGroupIds.BclThreading,
        ["System.Threading.Tasks"] = FrameworkApiGroupIds.BclThreading,
        ["System.Timers"] = FrameworkApiGroupIds.BclThreading
    };

    private static readonly IReadOnlyDictionary<string, string> AllowedNamespaces = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["System.Collections"] = FrameworkApiGroupIds.BclCollections,
        ["System.Collections.Generic"] = FrameworkApiGroupIds.BclCollections,
        ["System.Buffers"] = FrameworkApiGroupIds.BclMemory,
        ["System.Numerics"] = FrameworkApiGroupIds.BclMath,
        ["System.Text"] = FrameworkApiGroupIds.BclText
    };

    private static readonly IReadOnlyDictionary<string, string> DeniedTypeNames = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Console"] = FrameworkApiGroupIds.BclConsole,
        ["Activator"] = FrameworkApiGroupIds.BclReflection,
        ["AppDomain"] = FrameworkApiGroupIds.BclReflection,
        ["Attribute"] = FrameworkApiGroupIds.BclReflection,
        ["Delegate"] = FrameworkApiGroupIds.BclReflection,
        ["Directory"] = FrameworkApiGroupIds.BclFilesystem,
        ["DirectoryInfo"] = FrameworkApiGroupIds.BclFilesystem,
        ["Environment"] = FrameworkApiGroupIds.BclEnvironment,
        ["File"] = FrameworkApiGroupIds.BclFilesystem,
        ["FileInfo"] = FrameworkApiGroupIds.BclFilesystem,
        ["FileStream"] = FrameworkApiGroupIds.BclFilesystem,
        ["HttpClient"] = FrameworkApiGroupIds.BclNetworking,
        ["Path"] = FrameworkApiGroupIds.BclFilesystem,
        ["Process"] = FrameworkApiGroupIds.BclProcess,
        ["ProcessStartInfo"] = FrameworkApiGroupIds.BclProcess,
        ["Socket"] = FrameworkApiGroupIds.BclNetworking,
        ["StreamReader"] = FrameworkApiGroupIds.BclFilesystem,
        ["StreamWriter"] = FrameworkApiGroupIds.BclFilesystem,
        ["CancellationTokenSource"] = FrameworkApiGroupIds.BclThreading,
        ["Monitor"] = FrameworkApiGroupIds.BclThreading,
        ["SemaphoreSlim"] = FrameworkApiGroupIds.BclThreading,
        ["Task"] = FrameworkApiGroupIds.BclThreading,
        ["Thread"] = FrameworkApiGroupIds.BclThreading,
        ["ThreadPool"] = FrameworkApiGroupIds.BclThreading,
        ["Timer"] = FrameworkApiGroupIds.BclThreading,
        ["Type"] = FrameworkApiGroupIds.BclReflection
    };

    private static readonly IReadOnlyDictionary<string, string> AllowedTypeNames = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Array"] = FrameworkApiGroupIds.BclPrimitives,
        ["Boolean"] = FrameworkApiGroupIds.BclPrimitives,
        ["Char"] = FrameworkApiGroupIds.BclPrimitives,
        ["DateOnly"] = FrameworkApiGroupIds.BclTime,
        ["DateTime"] = FrameworkApiGroupIds.BclTime,
        ["Decimal"] = FrameworkApiGroupIds.BclPrimitives,
        ["Double"] = FrameworkApiGroupIds.BclPrimitives,
        ["Int16"] = FrameworkApiGroupIds.BclPrimitives,
        ["Int32"] = FrameworkApiGroupIds.BclPrimitives,
        ["Int64"] = FrameworkApiGroupIds.BclPrimitives,
        ["Math"] = FrameworkApiGroupIds.BclMath,
        ["Memory"] = FrameworkApiGroupIds.BclMemory,
        ["ReadOnlyMemory"] = FrameworkApiGroupIds.BclMemory,
        ["ReadOnlySpan"] = FrameworkApiGroupIds.BclMemory,
        ["Single"] = FrameworkApiGroupIds.BclPrimitives,
        ["Span"] = FrameworkApiGroupIds.BclMemory,
        ["String"] = FrameworkApiGroupIds.BclPrimitives,
        ["StringBuilder"] = FrameworkApiGroupIds.BclText,
        ["TimeOnly"] = FrameworkApiGroupIds.BclTime,
        ["TimeSpan"] = FrameworkApiGroupIds.BclTime
    };

    private static readonly HashSet<string> DeniedHostControlTypes = new(StringComparer.Ordinal)
    {
        "Octaryn.Shared.Host.HostCommand",
        "Octaryn.Shared.Host.HostCommandWriteScope",
        "Octaryn.Shared.Host.HostFrameContext",
        "Octaryn.Shared.Host.HostFrameSnapshot",
        "Octaryn.Shared.Host.HostInputSnapshot",
        "Octaryn.Shared.Host.HostModuleContext",
        "Octaryn.Shared.Host.HostSchedulerDiagnostics",
        "Octaryn.Shared.Host.HostScheduledWork",
        "Octaryn.Shared.Host.HostScheduledWorkContext",
        "Octaryn.Shared.Host.HostSchedulingContract",
        "Octaryn.Shared.Host.HostWorkAccess",
        "Octaryn.Shared.Host.IHostCommandSink",
        "Octaryn.Shared.Host.IHostScheduler",
        "Octaryn.Shared.World.BlockEdit",
        "Octaryn.Shared.World.ChunkConstants",
        "Octaryn.Shared.World.ChunkPosition",
        "Octaryn.Shared.World.ChunkSnapshot",
        "Schedulers.JobScheduler",
        "Schedulers.JobHandle"
    };

    private static readonly HashSet<string> DeniedModuleApiNamespaces = new(StringComparer.Ordinal)
    {
        "Octaryn.Shared.Networking",
        "Schedulers"
    };

    public static int Run(IReadOnlyList<string> args)
    {
        var selfTestErrors = RunSelfTests();
        if (selfTestErrors.Count > 0)
        {
            foreach (var error in selfTestErrors)
            {
                Console.Error.WriteLine($"module API probe self-test: {error}");
            }

            return 1;
        }

        var moduleRoot = ParseSourceRoot(args);
        var assetsPath = ParseOption(args, "--assets-file");
        var errors = Validate(moduleRoot, assetsPath);
        if (errors.Count == 0)
        {
            return 0;
        }

        foreach (var error in errors)
        {
            Console.Error.WriteLine($"module API probe: {error}");
        }

        return 1;
    }

    private static List<string> Validate(string moduleRoot, string? assetsPath = null)
    {
        var errors = new List<string>();
        var requestedGroups = LoadRequestedFrameworkGroups(moduleRoot, assetsPath, errors);

        foreach (var deniedGroup in DeniedFrameworkApiGroups.Values)
        {
            if (requestedGroups.Contains(deniedGroup))
            {
                errors.Add($"module manifest requests denied framework API group {deniedGroup}");
            }
        }

        foreach (var requestedGroup in requestedGroups)
        {
            if (!FrameworkApiGroupAllowlist.IsAllowed(requestedGroup))
            {
                errors.Add($"module manifest requests unapproved framework API group {requestedGroup}");
            }
        }

        var sourceRoot = Path.Combine(moduleRoot, "Source");
        var sourceFiles = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Contains("bin") &&
                !path.Split(Path.DirectorySeparatorChar).Contains("obj"))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var syntaxTrees = sourceFiles
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), parseOptions, path))
            .ToArray();
        var compilation = CSharpCompilation.Create(
            "Octaryn.ModuleApiProbe.Input",
            syntaxTrees,
            CompilationReferences(moduleRoot, assetsPath),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        foreach (var tree in syntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
            var root = tree.GetCompilationUnitRoot();
            ValidateUsingDirectives(errors, tree, root, requestedGroups);
            ValidateAllowedSyntax(errors, tree, root, requestedGroups);
            ValidateAttributes(errors, tree, root);
            ValidateReflectionSyntax(errors, tree, root);
            ValidateUnsafeSyntax(errors, tree, root);
            ValidateThreadingSyntax(errors, tree, root);
            ValidateIdentifierSymbols(errors, tree, semanticModel, root, requestedGroups);
        }

        foreach (var diagnostic in compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            errors.Add($"{diagnostic.Location.GetLineSpan().Path}:{diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1}: compile error {diagnostic.Id}: {diagnostic.GetMessage()}");
        }

        return errors;
    }

    private static List<string> RunSelfTests()
    {
        var errors = new List<string>();
        VerifyFrameworkGroupClassification(errors);
        VerifyDenied("filesystem using", "using System.IO;", FrameworkApiGroupIds.BclFilesystem, errors);
        VerifyDenied("filesystem alias", "using FileAlias = System.IO.File;", FrameworkApiGroupIds.BclFilesystem, errors);
        VerifyDenied(
            "filesystem static using",
            "using static System.IO.File;",
            FrameworkApiGroupIds.BclFilesystem,
            errors);
        VerifyDenied(
            "filesystem global type",
            "public static class Probe { public static void Run() { _ = global::System.IO.File.ReadAllText(\"x\"); } }",
            FrameworkApiGroupIds.BclFilesystem,
            errors);
        VerifyDenied(
            "environment global type",
            "public static class Probe { public static void Run() { _ = global::System.Environment.GetEnvironmentVariable(\"X\"); } }",
            FrameworkApiGroupIds.BclEnvironment,
            errors);
        VerifyDenied(
            "denied framework group constant",
            string.Empty,
            FrameworkApiGroupIds.BclFilesystem,
            errors,
            requestedGroups: ["FrameworkApiGroupIds.BclFilesystem"]);
        VerifyDenied(
            "denied framework group literal",
            string.Empty,
            FrameworkApiGroupIds.BclFilesystem,
            errors,
            requestedGroups: ["\"bcl.filesystem\""]);
        VerifyDenied(
            "networking contract namespace",
            "using Octaryn.Shared.Networking; public static class Probe { public static void Run() { _ = default(ClientCommandFrame); } }",
            "denied module API namespace",
            errors);
        ExpectValid(
            "requested allowed text API",
            "using System.Text; public static class Probe { public static void Run() { _ = new StringBuilder(); } }",
            ["FrameworkApiGroupIds.BclPrimitives", "FrameworkApiGroupIds.BclText"],
            errors);
        ExpectValid(
            "requested allowed collection API",
            "using System.Collections.Generic; public static class Probe { public static void Run() { _ = new List<int>(); } }",
            ["FrameworkApiGroupIds.BclPrimitives", "FrameworkApiGroupIds.BclCollections"],
            errors);
        ExpectValid(
            "requested non-generic collection API",
            "using System.Collections; public static class Probe { public static void Run() { _ = new ArrayList(); } }",
            ["FrameworkApiGroupIds.BclPrimitives", "FrameworkApiGroupIds.BclCollections"],
            errors);
        ExpectValid(
            "requested allowed math API",
            "public static class Probe { public static void Run() { _ = System.Math.Abs(-1); } }",
            ["FrameworkApiGroupIds.BclPrimitives", "FrameworkApiGroupIds.BclMath"],
            errors);
        ExpectValid(
            "requested allowed time API",
            "public static class Probe { public static void Run() { _ = System.DateTime.UtcNow; } }",
            ["FrameworkApiGroupIds.BclPrimitives", "FrameworkApiGroupIds.BclTime"],
            errors);
        ExpectValid(
            "manifest extractor ignores stray denied string",
            "public static class Probe { private const string Stray = \"bcl.filesystem\"; }",
            ["FrameworkApiGroupIds.BclPrimitives"],
            errors);
        ExpectValid(
            "manifest extractor ignores stray denied constant",
            "public static class Probe { private static readonly string Stray = FrameworkApiGroupIds.BclFilesystem; }",
            ["FrameworkApiGroupIds.BclPrimitives"],
            errors);
        VerifyDenied(
            "unrequested allowed namespace",
            "using System.Text;",
            "unrequested framework API group bcl.text",
            errors);
        VerifyDenied(
            "unrequested allowed type",
            "public static class Probe { public static void Run() { _ = new System.Text.StringBuilder(); } }",
            "unrequested framework API group bcl.text",
            errors);
        VerifyDenied(
            "unrequested collection API",
            "using System.Collections.Generic; public static class Probe { public static void Run() { _ = new List<int>(); } }",
            "unrequested framework API group bcl.collections",
            errors);
        VerifyDenied(
            "decoy requested group does not grant API",
            "using System.Text; public static class Probe { public static void Decoy() { Register(RequestedFrameworkApiGroups: [FrameworkApiGroupIds.BclText]); } private static void Register(string[] RequestedFrameworkApiGroups) { } }",
            "unrequested framework API group bcl.text",
            errors);
        VerifyDeniedRaw(
            "fake manifest type does not grant API",
            """
            using System.Text;
            using Octaryn.Shared.FrameworkAllowlist;

            namespace FakeModule
            {
                public sealed class GameModuleManifest
                {
                    public GameModuleManifest(string[] RequestedFrameworkApiGroups) { }
                }

                public static class FakeRegistration
                {
                    public static GameModuleManifest Manifest { get; } = new(RequestedFrameworkApiGroups: [FrameworkApiGroupIds.BclText]);
                }
            }

            internal static class RealRegistration
            {
                public static Octaryn.Shared.GameModules.GameModuleManifest Manifest { get; } = new(
                    ModuleId: "octaryn.test",
                    DisplayName: "Octaryn Test",
                    Version: "0.1.0",
                    OctarynApiVersion: "0.1.0",
                    RequiredCapabilities: [],
                    RequestedHostApis: [],
                    RequestedRuntimePackages: [],
                    RequestedBuildPackages: [],
                    RequestedFrameworkApiGroups: [FrameworkApiGroupIds.BclPrimitives],
                    ModuleDependencies: [],
                    ContentDeclarations: [],
                    AssetDeclarations: [],
                    Schedule: new Octaryn.Shared.GameModules.GameModuleScheduleDeclaration([]),
                    Compatibility: new Octaryn.Shared.GameModules.GameModuleCompatibility("0.1.0", "0.1.0", "octaryn.test.save.v0", SupportsMultiplayer: false));
            }

            public static class Probe { public static void Run() { _ = new StringBuilder(); } }
            """,
            "unrequested framework API group bcl.text",
            errors);
        ExpectValidRaw(
            "fully qualified manifest construction",
            """
            using System.Text;
            using Octaryn.Shared.FrameworkAllowlist;

            internal static class RealRegistration
            {
                public static Octaryn.Shared.GameModules.GameModuleManifest Manifest { get; } =
                    new Octaryn.Shared.GameModules.GameModuleManifest(
                        ModuleId: "octaryn.test",
                        DisplayName: "Octaryn Test",
                        Version: "0.1.0",
                        OctarynApiVersion: "0.1.0",
                        RequiredCapabilities: [],
                        RequestedHostApis: [],
                        RequestedRuntimePackages: [],
                        RequestedBuildPackages: [],
                        RequestedFrameworkApiGroups: [FrameworkApiGroupIds.BclPrimitives, FrameworkApiGroupIds.BclText],
                        ModuleDependencies: [],
                        ContentDeclarations: [],
                        AssetDeclarations: [],
                        Schedule: new Octaryn.Shared.GameModules.GameModuleScheduleDeclaration([]),
                        Compatibility: new Octaryn.Shared.GameModules.GameModuleCompatibility("0.1.0", "0.1.0", "octaryn.test.save.v0", SupportsMultiplayer: false));
            }

            public static class Probe { public static void Run() { _ = new StringBuilder(); } }
            """,
            errors);
        VerifyDenied(
            "unclassified system API",
            "using System.Globalization; public static class Probe { public static void Run() { _ = CultureInfo.InvariantCulture; } }",
            "unclassified framework API",
            errors);
        VerifyDenied(
            "unrequested math API",
            "public static class Probe { public static void Run() { _ = System.Math.Abs(-1); } }",
            "unrequested framework API group bcl.math",
            errors);
        VerifyDenied(
            "unrequested time API",
            "public static class Probe { public static void Run() { _ = System.DateTime.UtcNow; } }",
            "unrequested framework API group bcl.time",
            errors);
        VerifyDenied(
            "reflection type metadata",
            "public static class Probe { public static void Run() { _ = typeof(Probe).GetMethods(); } }",
            FrameworkApiGroupIds.BclReflection,
            errors);
        VerifyDenied(
            "reflection typeof expression",
            "public static class Probe { public static void Run() { _ = typeof(Probe); } }",
            FrameworkApiGroupIds.BclReflection,
            errors);
        VerifyDenied(
            "reflection get type",
            "public static class Probe { public static void Run(object value) { _ = value.GetType(); } }",
            FrameworkApiGroupIds.BclReflection,
            errors);
        VerifyDenied(
            "reflection activator",
            "public static class Probe { public static void Run() { _ = global::System.Activator.CreateInstance(typeof(Probe)); } }",
            FrameworkApiGroupIds.BclReflection,
            errors);
        VerifyDenied(
            "reflection delegate",
            "public static class Probe { public static void Run() { _ = global::System.Delegate.CreateDelegate(typeof(System.Action), null, \"x\", false); } }",
            FrameworkApiGroupIds.BclReflection,
            errors);
        VerifyDenied(
            "reflection app domain",
            "public static class Probe { public static void Run() { _ = global::System.AppDomain.CurrentDomain.GetAssemblies(); } }",
            FrameworkApiGroupIds.BclReflection,
            errors);
        VerifyDenied(
            "runtime code generation expressions",
            "public static class Probe { public static void Run() { _ = System.Linq.Expressions.Expression.Empty(); } }",
            FrameworkApiGroupIds.BclRuntimeCodeGeneration,
            errors);
        VerifyDenied(
            "process start info",
            "public static class Probe { public static void Run() { _ = new global::System.Diagnostics.ProcessStartInfo(\"x\"); } }",
            FrameworkApiGroupIds.BclProcess,
            errors);
        VerifyDenied(
            "process start",
            "public static class Probe { public static void Run() { _ = global::System.Diagnostics.Process.Start(\"x\"); } }",
            FrameworkApiGroupIds.BclProcess,
            errors);
        VerifyDenied(
            "thread global type",
            "public static class Probe { public static void Run() { global::System.Threading.Thread.Sleep(1); } }",
            FrameworkApiGroupIds.BclThreading,
            errors);
        VerifyDenied(
            "task global type",
            "public static class Probe { public static void Run() { _ = new global::System.Threading.Tasks.Task(() => { }); } }",
            FrameworkApiGroupIds.BclThreading,
            errors);
        VerifyDenied(
            "task run",
            "public static class Probe { public static void Run() { _ = global::System.Threading.Tasks.Task.Run(() => { }); } }",
            FrameworkApiGroupIds.BclThreading,
            errors);
        VerifyDenied(
            "parallel for",
            "public static class Probe { public static void Run() { global::System.Threading.Tasks.Parallel.For(0, 1, _ => { }); } }",
            FrameworkApiGroupIds.BclThreading,
            errors);
        VerifyDenied(
            "thread pool queue",
            "public static class Probe { public static void Run() { global::System.Threading.ThreadPool.QueueUserWorkItem(_ => { }); } }",
            FrameworkApiGroupIds.BclThreading,
            errors);
        VerifyDenied(
            "monitor enter",
            "public static class Probe { public static void Run(object gate) { global::System.Threading.Monitor.Enter(gate); } }",
            FrameworkApiGroupIds.BclThreading,
            errors);
        VerifyDenied(
            "thread static",
            "public static class Probe { [System.ThreadStatic] private static int Value; }",
            FrameworkApiGroupIds.BclThreading,
            errors);
        VerifyDenied(
            "lock statement",
            "public static class Probe { public static void Run(object gate) { lock (gate) { } } }",
            FrameworkApiGroupIds.BclThreading,
            errors);
        VerifyDenied(
            "native import attribute",
            "using System.Runtime.InteropServices; public static partial class Probe { [DllImport(\"x\")] public static extern void Run(); }",
            FrameworkApiGroupIds.BclNativeInterop,
            errors);
        VerifyDenied(
            "fully qualified native import attribute",
            "public static partial class Probe { [System.Runtime.InteropServices.DllImport(\"x\")] public static extern void Run(); }",
            FrameworkApiGroupIds.BclNativeInterop,
            errors);
        VerifyDenied(
            "unsafe pointer",
            "public static unsafe class Probe { public static void Run() { int* value = stackalloc int[1]; } }",
            FrameworkApiGroupIds.BclUnsafeCode,
            errors);
        VerifyDenied(
            "unsafe modifier",
            "public static unsafe class Probe { public static void Run() { } }",
            FrameworkApiGroupIds.BclUnsafeCode,
            errors);
        VerifyDenied(
            "fixed statement",
            "public static unsafe class Probe { public static void Run(char[] input) { fixed (char* value = input) { } } }",
            FrameworkApiGroupIds.BclUnsafeCode,
            errors);
        VerifyDenied(
            "function pointer",
            "public static unsafe class Probe { public static void Run() { delegate*<void> value = null; } }",
            FrameworkApiGroupIds.BclUnsafeCode,
            errors);
        VerifyDenied(
            "host command write scope",
            "using Octaryn.Shared.Host; public static class Probe { public static void Run() { _ = HostCommandWriteScope.Enter(HostWorkAccess.CommandSinkWrite); } }",
            "denied host control API",
            errors);
        VerifyDenied(
            "host work access command sink",
            "using Octaryn.Shared.Host; public static class Probe { public static void Run() { _ = HostWorkAccess.CommandSinkWrite; } }",
            "denied host control API",
            errors);
        VerifyDenied(
            "host command primitive",
            "using Octaryn.Shared.Host; public static class Probe { public static void Run() { _ = default(HostCommand); } }",
            "denied host control API",
            errors);
        VerifyDenied(
            "host frame snapshot primitive",
            "using Octaryn.Shared.Host; public static class Probe { public static void Run() { _ = default(HostFrameSnapshot); } }",
            "denied host control API",
            errors);
        VerifyDenied(
            "chunk snapshot primitive",
            "using Octaryn.Shared.World; public static class Probe { public static void Run() { _ = default(ChunkSnapshot); } }",
            "denied host control API",
            errors);
        VerifyDenied(
            "transitive scheduler namespace",
            "using Schedulers; public static class Probe { public static void Run() { } }",
            "denied module API namespace",
            errors);
        VerifyDeniedRaw(
            "unresolved type compile diagnostic",
            "public static class Probe { public static void Run() { MissingType value = null; } }",
            "compile error CS0246",
            errors);
        VerifyDeniedRaw(
            "syntax compile diagnostic",
            "public static class Probe { public static void Run() { int value = 1 } }",
            "compile error CS1002",
            errors);

        return errors;
    }

    private static void VerifyFrameworkGroupClassification(List<string> errors)
    {
        var allowed = new HashSet<string>(FrameworkApiGroupAllowlist.Values, StringComparer.Ordinal);
        var denied = new HashSet<string>(DeniedFrameworkApiGroups.Values, StringComparer.Ordinal);
        foreach (var field in typeof(FrameworkApiGroupIds).GetFields())
        {
            if (field.GetValue(null) is not string value)
            {
                continue;
            }

            var isAllowed = allowed.Contains(value);
            var isDenied = denied.Contains(value);
            if (isAllowed == isDenied)
            {
                errors.Add($"framework group {value} must be classified as exactly one of allowed or denied");
            }
        }
    }

    private static void VerifyDenied(
        string name,
        string source,
        string expectedText,
        List<string> errors,
        IReadOnlyList<string>? requestedGroups = null)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"octaryn-module-api-probe-{Guid.NewGuid():N}");
        try
        {
            var sourceRoot = Path.Combine(tempRoot, "Source", "Module");
            Directory.CreateDirectory(sourceRoot);
            File.WriteAllText(Path.Combine(sourceRoot, "Probe.cs"), SelfTestSource(source, requestedGroups));

            var validationErrors = Validate(tempRoot);
            if (!validationErrors.Any(error => error.Contains(expectedText, StringComparison.Ordinal)))
            {
                errors.Add($"{name} did not report {expectedText}");
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

    private static void ExpectValid(
        string name,
        string source,
        IReadOnlyList<string> requestedGroups,
        List<string> errors)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"octaryn-module-api-probe-{Guid.NewGuid():N}");
        try
        {
            var sourceRoot = Path.Combine(tempRoot, "Source", "Module");
            Directory.CreateDirectory(sourceRoot);
            File.WriteAllText(Path.Combine(sourceRoot, "Probe.cs"), SelfTestSource(source, requestedGroups));

            var validationErrors = Validate(tempRoot);
            if (validationErrors.Count > 0)
            {
                errors.Add($"{name} expected valid source, got {string.Join(", ", validationErrors)}");
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

    private static void VerifyDeniedRaw(
        string name,
        string source,
        string expectedText,
        List<string> errors)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"octaryn-module-api-probe-{Guid.NewGuid():N}");
        try
        {
            var sourceRoot = Path.Combine(tempRoot, "Source", "Module");
            Directory.CreateDirectory(sourceRoot);
            File.WriteAllText(Path.Combine(sourceRoot, "Probe.cs"), source);

            var validationErrors = Validate(tempRoot);
            if (!validationErrors.Any(error => error.Contains(expectedText, StringComparison.Ordinal)))
            {
                errors.Add($"{name} did not report {expectedText}");
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

    private static void ExpectValidRaw(string name, string source, List<string> errors)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"octaryn-module-api-probe-{Guid.NewGuid():N}");
        try
        {
            var sourceRoot = Path.Combine(tempRoot, "Source", "Module");
            Directory.CreateDirectory(sourceRoot);
            File.WriteAllText(Path.Combine(sourceRoot, "Probe.cs"), source);

            var validationErrors = Validate(tempRoot);
            if (validationErrors.Count > 0)
            {
                errors.Add($"{name} expected valid source, got {string.Join(", ", validationErrors)}");
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

    private static string SelfTestSource(string source, IReadOnlyList<string>? requestedGroups = null)
    {
        var groups = string.Join(
            "," + Environment.NewLine + "                    ",
            requestedGroups ?? ["FrameworkApiGroupIds.BclPrimitives"]);

        return $$"""
            using Octaryn.Shared.FrameworkAllowlist;
            using Octaryn.Shared.GameModules;

            {{source}}

            internal static class ModuleFrameworkGroups
            {
                public static GameModuleManifest Manifest { get; } = new(
                    ModuleId: "octaryn.test",
                    DisplayName: "Octaryn Test",
                    Version: "0.1.0",
                    OctarynApiVersion: "0.1.0",
                    RequiredCapabilities: [],
                    RequestedHostApis: [],
                    RequestedRuntimePackages: [],
                    RequestedBuildPackages: [],
                    RequestedFrameworkApiGroups:
                    [
                        {{groups}}
                    ],
                    ModuleDependencies: [],
                    ContentDeclarations: [],
                    AssetDeclarations: [],
                    Schedule: new GameModuleScheduleDeclaration([]),
                    Compatibility: new GameModuleCompatibility("0.1.0", "0.1.0", "octaryn.test.save.v0", SupportsMultiplayer: false));
            }
            """;
    }

    private static string ParseSourceRoot(IReadOnlyList<string> args)
    {
        for (var index = 0; index < args.Count; index++)
        {
            if (args[index] == "--source-root" && index + 1 < args.Count)
            {
                return Path.GetFullPath(args[index + 1]);
            }
        }

        for (var index = 0; index < args.Count; index++)
        {
            if (args[index].StartsWith("--", StringComparison.Ordinal))
            {
                index++;
                continue;
            }

            return Path.GetFullPath(args[index]);
        }

        return Path.GetFullPath("octaryn-basegame");
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

    private static HashSet<string> LoadRequestedFrameworkGroups(string moduleRoot, string? assetsPath, List<string> errors)
    {
        var groups = new HashSet<string>(StringComparer.Ordinal);
        var sourceRoot = Path.Combine(moduleRoot, "Source");
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var syntaxTrees = new List<SyntaxTree>();
        foreach (var path in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (path.Split(Path.DirectorySeparatorChar).Contains("bin") ||
                path.Split(Path.DirectorySeparatorChar).Contains("obj"))
            {
                continue;
            }

            var text = File.ReadAllText(path);
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(text, parseOptions, path));
        }

        var compilation = CSharpCompilation.Create(
            "Octaryn.ModuleApiProbe.ManifestInput",
            syntaxTrees,
            CompilationReferences(moduleRoot, assetsPath),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        foreach (var tree in syntaxTrees)
        {
            var path = tree.FilePath;
            var root = tree.GetCompilationUnitRoot();
            var semanticModel = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
            foreach (var manifestCreation in ManifestCreations(root, semanticModel))
            {
                LoadFrameworkGroupsFromManifestCreation(path, errors, groups, manifestCreation);
            }
        }

        if (groups.Count == 0)
        {
            errors.Add($"{moduleRoot}: no requested framework API groups found in module source.");
        }

        return groups;
    }

    private static IEnumerable<BaseObjectCreationExpressionSyntax> ManifestCreations(
        CompilationUnitSyntax root,
        SemanticModel semanticModel)
    {
        foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            if (IsGameModuleManifestCreation(semanticModel, creation))
            {
                yield return creation;
            }
        }

        foreach (var implicitCreation in root.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>())
        {
            if (IsGameModuleManifestCreation(semanticModel, implicitCreation))
            {
                yield return implicitCreation;
            }
        }
    }

    private static bool IsGameModuleManifestCreation(
        SemanticModel semanticModel,
        BaseObjectCreationExpressionSyntax creation)
    {
        return semanticModel.GetTypeInfo(creation).Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            "global::Octaryn.Shared.GameModules.GameModuleManifest";
    }

    private static void LoadFrameworkGroupsFromManifestCreation(
        string path,
        List<string> errors,
        HashSet<string> groups,
        BaseObjectCreationExpressionSyntax manifestCreation)
    {
        foreach (var argument in manifestCreation.ArgumentList?.Arguments ?? [])
        {
            if (argument.NameColon?.Name.Identifier.ValueText != "RequestedFrameworkApiGroups")
            {
                continue;
            }

            foreach (var memberAccess in argument.Expression.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>())
            {
                if (!memberAccess.ToString().Contains("FrameworkApiGroupIds.", StringComparison.Ordinal))
                {
                    continue;
                }

                var value = ResolveFrameworkGroup(memberAccess.Name.Identifier.ValueText);
                if (value is null)
                {
                    errors.Add($"{path}: unknown FrameworkApiGroupIds constant {memberAccess.Name.Identifier.ValueText}");
                    continue;
                }

                groups.Add(value);
            }

            foreach (var literal in argument.Expression.DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>())
            {
                if (!literal.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    continue;
                }

                var value = literal.Token.ValueText;
                if (value.StartsWith("bcl.", StringComparison.Ordinal))
                {
                    groups.Add(value);
                }
            }
        }
    }

    private static string? ResolveFrameworkGroup(string constantName)
    {
        return constantName switch
        {
            nameof(FrameworkApiGroupIds.BclPrimitives) => FrameworkApiGroupIds.BclPrimitives,
            nameof(FrameworkApiGroupIds.BclCollections) => FrameworkApiGroupIds.BclCollections,
            nameof(FrameworkApiGroupIds.BclMemory) => FrameworkApiGroupIds.BclMemory,
            nameof(FrameworkApiGroupIds.BclMath) => FrameworkApiGroupIds.BclMath,
            nameof(FrameworkApiGroupIds.BclTime) => FrameworkApiGroupIds.BclTime,
            nameof(FrameworkApiGroupIds.BclText) => FrameworkApiGroupIds.BclText,
            nameof(FrameworkApiGroupIds.BclFilesystem) => FrameworkApiGroupIds.BclFilesystem,
            nameof(FrameworkApiGroupIds.BclNetworking) => FrameworkApiGroupIds.BclNetworking,
            nameof(FrameworkApiGroupIds.BclProcess) => FrameworkApiGroupIds.BclProcess,
            nameof(FrameworkApiGroupIds.BclReflection) => FrameworkApiGroupIds.BclReflection,
            nameof(FrameworkApiGroupIds.BclRuntimeCodeGeneration) => FrameworkApiGroupIds.BclRuntimeCodeGeneration,
            nameof(FrameworkApiGroupIds.BclNativeInterop) => FrameworkApiGroupIds.BclNativeInterop,
            nameof(FrameworkApiGroupIds.BclThreading) => FrameworkApiGroupIds.BclThreading,
            nameof(FrameworkApiGroupIds.BclEnvironment) => FrameworkApiGroupIds.BclEnvironment,
            nameof(FrameworkApiGroupIds.BclConsole) => FrameworkApiGroupIds.BclConsole,
            nameof(FrameworkApiGroupIds.BclUnsafeCode) => FrameworkApiGroupIds.BclUnsafeCode,
            _ => null
        };
    }

    private static IEnumerable<MetadataReference> CompilationReferences(string moduleRoot, string? assetsPath)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var paths = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var path in paths)
        {
            if (seen.Add(path))
            {
                yield return MetadataReference.CreateFromFile(path);
            }
        }

        if (seen.Add(typeof(GameModuleManifest).Assembly.Location))
        {
            yield return MetadataReference.CreateFromFile(typeof(GameModuleManifest).Assembly.Location);
        }

        foreach (var path in ModulePackageReferencePaths(moduleRoot, assetsPath))
        {
            if (seen.Add(path))
            {
                yield return MetadataReference.CreateFromFile(path);
            }
        }
    }

    private static IEnumerable<string> ModulePackageReferencePaths(string moduleRoot, string? assetsPath)
    {
        assetsPath ??= ResolveProjectAssetsPath(moduleRoot);
        if (assetsPath is null)
        {
            yield break;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(assetsPath));
        var packageFolders = document.RootElement.GetProperty("packageFolders")
            .EnumerateObject()
            .Select(folder => folder.Name)
            .ToArray();

        foreach (var target in document.RootElement.GetProperty("targets").EnumerateObject())
        {
            foreach (var package in target.Value.EnumerateObject())
            {
                if (!package.Name.Contains('/', StringComparison.Ordinal))
                {
                    continue;
                }

                var packageParts = package.Name.Split('/', 2);
                foreach (var assetGroupName in new[] { "compile", "runtime" })
                {
                    if (!package.Value.TryGetProperty(assetGroupName, out var assets))
                    {
                        continue;
                    }

                    foreach (var asset in assets.EnumerateObject())
                    {
                        if (!asset.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                            asset.Name.EndsWith("/_._", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        foreach (var packageFolder in packageFolders)
                        {
                            var path = Path.Combine(packageFolder, packageParts[0].ToLowerInvariant(), packageParts[1], asset.Name);
                            if (File.Exists(path))
                            {
                                yield return path;
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    private static string? ResolveProjectAssetsPath(string moduleRoot)
    {
        var projectFile = Directory.EnumerateFiles(moduleRoot, "*.csproj", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.Ordinal)
            .FirstOrDefault();
        if (projectFile is null)
        {
            return null;
        }

        var repoRoot = FindRepoRoot(moduleRoot);
        var candidate = Path.Combine(repoRoot, "build", "debug-linux", "basegame", "managed-obj", "project.assets.json");
        if (File.Exists(candidate))
        {
            return candidate;
        }

        return null;
    }

    private static string FindRepoRoot(string moduleRoot)
    {
        var directory = new DirectoryInfo(moduleRoot);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")) &&
                Directory.Exists(Path.Combine(directory.FullName, "tools", "validation")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static void ValidateUsingDirectives(
        List<string> errors,
        SyntaxTree tree,
        CompilationUnitSyntax root,
        IReadOnlySet<string> requestedGroups)
    {
        foreach (var usingDirective in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            var name = NormalizeName(usingDirective.Name);
            if (name is null)
            {
                continue;
            }

            if (FindDeniedGroup(name) is { } group)
            {
                errors.Add($"{Location(tree, usingDirective.GetLocation())}: denied framework API group {group}: using {name}");
                continue;
            }

            if (IsDeniedModuleApiNamespace(name))
            {
                errors.Add($"{Location(tree, usingDirective.GetLocation())}: denied module API namespace: using {name}");
                continue;
            }

            if (FindAllowedGroup(name) is { } allowedGroup)
            {
                if (!requestedGroups.Contains(allowedGroup))
                {
                    errors.Add($"{Location(tree, usingDirective.GetLocation())}: unrequested framework API group {allowedGroup}: using {name}");
                }

                continue;
            }

            if (IsSystemApi(name))
            {
                errors.Add($"{Location(tree, usingDirective.GetLocation())}: unclassified framework API: using {name}");
            }
        }
    }

    private static void ValidateAttributes(
        List<string> errors,
        SyntaxTree tree,
        CompilationUnitSyntax root)
    {
        foreach (var attribute in root.DescendantNodes().OfType<AttributeSyntax>())
        {
            var name = NormalizeName(attribute.Name);
            if (name is "DllImport" or "DllImportAttribute" or "LibraryImport" or "LibraryImportAttribute")
            {
                errors.Add($"{Location(tree, attribute.GetLocation())}: denied framework API group {FrameworkApiGroupIds.BclNativeInterop}: {name}");
            }
        }
    }

    private static void ValidateAllowedSyntax(
        List<string> errors,
        SyntaxTree tree,
        CompilationUnitSyntax root,
        IReadOnlySet<string> requestedGroups)
    {
        foreach (var memberAccess in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            var expression = memberAccess.Expression.ToString()
                .Replace("global::", string.Empty, StringComparison.Ordinal);
            if (AllowedTypeNames.TryGetValue(expression.Split('.').Last(), out var group) &&
                !requestedGroups.Contains(group))
            {
                errors.Add($"{Location(tree, memberAccess.GetLocation())}: unrequested framework API group {group}: {expression}");
            }
        }
    }

    private static void ValidateReflectionSyntax(
        List<string> errors,
        SyntaxTree tree,
        CompilationUnitSyntax root)
    {
        foreach (var node in root.DescendantNodes().OfType<TypeOfExpressionSyntax>())
        {
            errors.Add($"{Location(tree, node.GetLocation())}: denied framework API group {FrameworkApiGroupIds.BclReflection}: typeof");
        }

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.ValueText == "GetType")
            {
                errors.Add($"{Location(tree, invocation.GetLocation())}: denied framework API group {FrameworkApiGroupIds.BclReflection}: GetType");
            }
        }
    }

    private static void ValidateUnsafeSyntax(
        List<string> errors,
        SyntaxTree tree,
        CompilationUnitSyntax root)
    {
        foreach (var node in root.DescendantNodes())
        {
            if (node is UnsafeStatementSyntax or FixedStatementSyntax or StackAllocArrayCreationExpressionSyntax or FunctionPointerTypeSyntax)
            {
                errors.Add($"{Location(tree, node.GetLocation())}: denied framework API group {FrameworkApiGroupIds.BclUnsafeCode}: {node.Kind()}");
            }
        }

        foreach (var token in root.DescendantTokens())
        {
            if (token.IsKind(SyntaxKind.UnsafeKeyword))
            {
                errors.Add($"{Location(tree, token.GetLocation())}: denied framework API group {FrameworkApiGroupIds.BclUnsafeCode}: unsafe");
            }
        }
    }

    private static void ValidateThreadingSyntax(
        List<string> errors,
        SyntaxTree tree,
        CompilationUnitSyntax root)
    {
        foreach (var node in root.DescendantNodes().OfType<LockStatementSyntax>())
        {
            errors.Add($"{Location(tree, node.GetLocation())}: denied framework API group {FrameworkApiGroupIds.BclThreading}: lock");
        }
    }

    private static void ValidateIdentifierSymbols(
        List<string> errors,
        SyntaxTree tree,
        SemanticModel semanticModel,
        CompilationUnitSyntax root,
        IReadOnlySet<string> requestedGroups)
    {
        foreach (var node in root.DescendantNodes())
        {
            if (node is IdentifierNameSyntax identifier)
            {
                ValidateSymbol(errors, tree, semanticModel, identifier, identifier.Identifier.ValueText, requestedGroups);
                continue;
            }

            if (node is MemberAccessExpressionSyntax memberAccess)
            {
                ValidateSymbol(errors, tree, semanticModel, memberAccess, memberAccess.Name.Identifier.ValueText, requestedGroups);
            }
        }
    }

    private static void ValidateSymbol(
        List<string> errors,
        SyntaxTree tree,
        SemanticModel semanticModel,
        SyntaxNode node,
        string fallbackName,
        IReadOnlySet<string> requestedGroups)
    {
        var symbol = semanticModel.GetSymbolInfo(node).Symbol ??
            semanticModel.GetTypeInfo(node).Type;
        var containingType = symbol switch
        {
            IMethodSymbol method => method.ContainingType,
            IPropertySymbol property => property.ContainingType,
            IFieldSymbol field => field.ContainingType,
            INamedTypeSymbol namedType => namedType,
            _ => null
        };

        if (containingType is not null)
        {
            var fullyQualifiedName = containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", string.Empty, StringComparison.Ordinal);
            if (FindDeniedGroup(fullyQualifiedName) is { } group)
            {
                errors.Add($"{Location(tree, node.GetLocation())}: denied framework API group {group}: {fullyQualifiedName}");
                return;
            }

            if (DeniedHostControlTypes.Contains(fullyQualifiedName))
            {
                errors.Add($"{Location(tree, node.GetLocation())}: denied host control API: {fullyQualifiedName}");
                return;
            }

            if (IsDeniedModuleApiNamespace(fullyQualifiedName))
            {
                errors.Add($"{Location(tree, node.GetLocation())}: denied module API namespace: {fullyQualifiedName}");
                return;
            }

            var allowedGroup = FindAllowedGroup(fullyQualifiedName) ?? FindAllowedTypeGroup(fullyQualifiedName);
            if (allowedGroup is not null)
            {
                if (!requestedGroups.Contains(allowedGroup))
                {
                    errors.Add($"{Location(tree, node.GetLocation())}: unrequested framework API group {allowedGroup}: {fullyQualifiedName}");
                }

                return;
            }

            if (IsSystemApi(fullyQualifiedName))
            {
                errors.Add($"{Location(tree, node.GetLocation())}: unclassified framework API: {fullyQualifiedName}");
            }

            return;
        }

        if (DeniedTypeNames.TryGetValue(fallbackName, out var fallbackGroup))
        {
            errors.Add($"{Location(tree, node.GetLocation())}: denied framework API group {fallbackGroup}: {fallbackName}");
            return;
        }

        if (AllowedTypeNames.TryGetValue(fallbackName, out var allowedFallbackGroup) &&
            !requestedGroups.Contains(allowedFallbackGroup))
        {
            errors.Add($"{Location(tree, node.GetLocation())}: unrequested framework API group {allowedFallbackGroup}: {fallbackName}");
        }
    }

    private static string? FindAllowedGroup(string fullyQualifiedName)
    {
        foreach (var (prefix, group) in AllowedNamespaces)
        {
            if (fullyQualifiedName == prefix ||
                fullyQualifiedName.StartsWith($"{prefix}.", StringComparison.Ordinal))
            {
                return group;
            }
        }

        return null;
    }

    private static string? FindAllowedTypeGroup(string fullyQualifiedName)
    {
        var typeName = fullyQualifiedName.Split('.').Last();
        return AllowedTypeNames.TryGetValue(typeName, out var group) ? group : null;
    }

    private static bool IsSystemApi(string fullyQualifiedName)
    {
        return fullyQualifiedName == "System" ||
            fullyQualifiedName.StartsWith("System.", StringComparison.Ordinal);
    }

    private static string? FindDeniedGroup(string fullyQualifiedName)
    {
        foreach (var (prefix, group) in DeniedNamespaces)
        {
            if (fullyQualifiedName == prefix ||
                fullyQualifiedName.StartsWith($"{prefix}.", StringComparison.Ordinal))
            {
                return group;
            }
        }

        return null;
    }

    private static bool IsDeniedModuleApiNamespace(string fullyQualifiedName)
    {
        return DeniedModuleApiNamespaces.Any(prefix =>
            fullyQualifiedName == prefix ||
            fullyQualifiedName.StartsWith($"{prefix}.", StringComparison.Ordinal));
    }

    private static string? NormalizeName(NameSyntax? name)
    {
        return name?.ToString()
            .Replace("global::", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    private static string Location(SyntaxTree tree, Location location)
    {
        var span = location.GetLineSpan();
        return $"{tree.FilePath}:{span.StartLinePosition.Line + 1}";
    }
}
