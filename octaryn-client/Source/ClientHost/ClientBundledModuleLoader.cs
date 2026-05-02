using System.Reflection;
using System.Runtime.Loader;
using Octaryn.Shared.GameModules;

namespace Octaryn.Client.ClientHost;

internal static class ClientBundledModuleLoader
{
    private const string BasegameAssemblyName = "Octaryn.Basegame";
    private const string BasegameRegistrationType = "Octaryn.Basegame.Module.BasegameModuleRegistration";
    private static bool s_resolverAttached;

    public static IGameModuleRegistration LoadBasegameRegistration()
    {
        AttachResolver();
        LoadAssembly("Octaryn.Shared");
        var assembly = LoadAssembly(BasegameAssemblyName);
        var type = assembly.GetType(BasegameRegistrationType, throwOnError: true)!;
        if (!typeof(IGameModuleRegistration).IsAssignableFrom(type))
        {
            throw new InvalidOperationException($"{BasegameRegistrationType} does not implement {nameof(IGameModuleRegistration)}.");
        }

        return Activator.CreateInstance(type) is IGameModuleRegistration registration
            ? registration
            : throw new InvalidOperationException($"{BasegameRegistrationType} could not be created.");
    }

    private static Assembly LoadAssembly(string assemblyName)
    {
        var assemblyPath = Path.Combine(ModuleDirectory, $"{assemblyName}.dll");
        if (File.Exists(assemblyPath))
        {
            return ModuleLoadContext.LoadFromAssemblyPath(assemblyPath);
        }

        return Assembly.Load(assemblyName);
    }

    private static string ModuleDirectory =>
        Path.GetDirectoryName(typeof(ClientBundledModuleLoader).Assembly.Location) ?? AppContext.BaseDirectory;

    private static AssemblyLoadContext ModuleLoadContext =>
        AssemblyLoadContext.GetLoadContext(typeof(ClientBundledModuleLoader).Assembly) ?? AssemblyLoadContext.Default;

    private static void AttachResolver()
    {
        if (s_resolverAttached)
        {
            return;
        }

        ModuleLoadContext.Resolving += ResolveFromBundleDirectory;
        s_resolverAttached = true;
    }

    private static Assembly? ResolveFromBundleDirectory(AssemblyLoadContext context, AssemblyName name)
    {
        var assemblyPath = Path.Combine(ModuleDirectory, $"{name.Name}.dll");
        return File.Exists(assemblyPath) ? context.LoadFromAssemblyPath(assemblyPath) : null;
    }
}
