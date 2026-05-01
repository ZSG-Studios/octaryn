using Octaryn.Basegame.Module;
using Octaryn.Server.Tick;
using Octaryn.Shared.GameModules;
using Octaryn.Shared.Host;

namespace Octaryn.Server;

internal sealed class ServerModuleActivator : IDisposable
{
    private readonly IGameModuleRegistration _registration;
    private readonly bool _requiresBundledMetadata;
    private ServerHostScheduler? _scheduler;
    private IGameModuleInstance? _instance;
    private bool _isDisposed;

    public ServerModuleActivator()
        : this(new BasegameModuleRegistration(), requiresBundledMetadata: true)
    {
    }

    public ServerModuleActivator(IGameModuleRegistration registration)
        : this(registration, requiresBundledMetadata: false)
    {
    }

    private ServerModuleActivator(IGameModuleRegistration registration, bool requiresBundledMetadata)
    {
        _registration = registration;
        _requiresBundledMetadata = requiresBundledMetadata;
    }

    public bool IsActive => _instance is not null;

    public int Activate(IHostCommandSink commandSink)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_instance is not null)
        {
            return 0;
        }

        var validationReport = ServerModuleValidation.Validate(_registration);
        if (!validationReport.IsValid)
        {
            return -2;
        }

        var bundledManifest = ServerBundledModuleCatalog.ResolveManifest(_registration.Manifest.ModuleId);
        if ((bundledManifest is null && _requiresBundledMetadata) ||
            (bundledManifest is not null && !BundledModuleMetadataVerifier.Matches(bundledManifest, _registration.Manifest)))
        {
            return -3;
        }

        var scheduler = new ServerHostScheduler(_registration.Manifest.Schedule.Systems);
        try
        {
            _instance = _registration.CreateInstance(HostModuleContext.Create(_registration.Manifest, commandSink));
            _scheduler = scheduler;
        }
        catch
        {
            scheduler.Dispose();
            throw;
        }

        return 0;
    }

    public void Tick(in HostFrameSnapshot snapshot)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        if (_instance is null || _scheduler is null)
        {
            return;
        }

        var frame = HostFrameContext.FromSnapshot(in snapshot);
        var moduleFrame = new ModuleFrameContext(frame.DeltaSeconds, frame.FrameIndex);
        var declaration = _registration.Manifest.Schedule.Systems[0];
        var work = HostScheduledWork.FromDeclaration(
            declaration,
            _ => _instance.Tick(in moduleFrame));
        if (!_scheduler.TryRun(work, frame))
        {
            throw new InvalidOperationException("Server module tick could not be scheduled by the host.");
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        try
        {
            _instance?.Dispose();
        }
        finally
        {
            _scheduler?.Dispose();
            _instance = null;
            _scheduler = null;
        }
    }
}
