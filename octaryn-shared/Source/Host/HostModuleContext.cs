namespace Octaryn.Shared.Host;

using Octaryn.Shared.ApiExposure;
using Octaryn.Shared.GameModules;

internal static class HostModuleContext
{
    public static ModuleHostContext Create(
        GameModuleManifest manifest,
        IHostCommandSink commands)
    {
        var requestedHostApis = manifest.RequestedHostApis ?? [];
        var schedule = manifest.Schedule ?? new GameModuleScheduleDeclaration([]);
        var grantsCommandSink = requestedHostApis.Contains(HostApiIds.Commands, StringComparer.Ordinal) &&
            HasScheduledWrite(schedule.Systems ?? [], HostApiIds.Commands);
        return new ModuleHostContext(
            grantsCommandSink
                ? new ScopedModuleCommandRequests(commands)
                : DeniedModuleCommandRequests.Instance);
    }

    private static bool HasScheduledWrite(
        IReadOnlyList<ScheduledSystemDeclaration> systems,
        string resourceId)
    {
        return systems.Any(system => (system.Writes ?? [])
            .Any(resource => resource.ResourceId == resourceId && resource.Mode == ScheduledAccessMode.Write));
    }

    private sealed class DeniedModuleCommandRequests : IModuleCommandRequests
    {
        public static readonly DeniedModuleCommandRequests Instance = new();

        private DeniedModuleCommandRequests()
        {
        }

        public bool TryRequest(ModuleCommandRequest request)
        {
            _ = request;
            return false;
        }
    }

    private sealed class ScopedModuleCommandRequests(IHostCommandSink inner) : IModuleCommandRequests
    {
        public bool TryRequest(ModuleCommandRequest request)
        {
            if (!HostCommandWriteScope.IsActive)
            {
                return false;
            }

            return inner.Enqueue(new HostCommand
            {
                Version = HostCommand.VersionValue,
                Size = HostCommand.SizeValue,
                Kind = HostCommandKind.None,
                RequestId = request.RequestId
            });
        }
    }
}
