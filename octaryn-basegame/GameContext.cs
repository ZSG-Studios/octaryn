using Arch.Core;
using Octaryn.Engine.Api;

namespace Octaryn.Game;

internal sealed class GameContext : IDisposable
{
    private readonly World _world = World.Create();
    private readonly OctarynNativeBridge _nativeBridge;
    private Entity _bootstrapEntity;
    private GameContext(OctarynNativeBridge nativeBridge)
    {
        _nativeBridge = nativeBridge;
    }

    public static GameContext Create(OctarynNativeBridge nativeBridge)
    {
        var context = new GameContext(nativeBridge);
        context.CreateBootstrapEntity();
        return context;
    }

    public void Tick(in OctarynFrameSnapshot snapshot)
    {
        var frame = OctarynFrameContext.FromSnapshot(in snapshot);
        _world.Set(_bootstrapEntity, frame);
    }

    public bool Enqueue(OctarynNativeCommand command)
    {
        return _nativeBridge.Enqueue(command);
    }

    public void Dispose()
    {
        _world.Dispose();
    }

    private void CreateBootstrapEntity()
    {
        _bootstrapEntity = _world.Create(
            new ManagedGameTag(),
            new OctarynFrameContext(0.0, 0, default, default));
    }
}
