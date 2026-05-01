namespace Octaryn.Server;

public static class ServerHost
{
    public static int Run(IReadOnlyList<string> args)
    {
        _ = args;
        using var basegame = new ServerModuleActivator();
        return basegame.Activate(new ServerConsoleCommandSink());
    }
}
