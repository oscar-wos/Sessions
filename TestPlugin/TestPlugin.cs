using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;
using Sessions.API;

namespace TestPlugin;

public class TestPlugin : BasePlugin
{
    public override string ModuleName => "TestPlugin";
    public override string ModuleVersion => "1.0.1";

    private static PlayerCapability<ISessionsPlayer> CapabilityPlayer { get; } =
        new("sessions:player");

    private static PluginCapability<ISessionsServer> CapabilityServer { get; } =
        new("sessions:server");

    public override void OnAllPluginsLoaded(bool isReload)
    {
        var server = CapabilityServer.Get()!.Server;

        if (server != null)
            Logger.LogInformation(
                $"Server: {server.Id} ({server.Ip}:{server.Port}) - Map: {server.MapName} [{server.Map!.Id}]"
            );

        AddCommand("css_test", "test", CommandTest);
    }

    private void CommandTest(CCSPlayerController? controller, CommandInfo info)
    {
        if (controller == null)
            return;

        var server = CapabilityServer.Get()!.Server;
        var player = CapabilityPlayer.Get(controller)!.Player;
        var session = CapabilityPlayer.Get(controller)!.Session;

        if (server == null || player == null || session == null)
            return;

        Logger.LogInformation(
            $"Player: {player.Id} - Session: {session.Id} - Server: {server.Id}/{server.MapName}[{server.Map!.Id}] ({server.Ip}:{server.Port}"
        );
    }
}
