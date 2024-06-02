using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;
using Sessions.API;

namespace TestPlugin;

public class TestPlugin : BasePlugin
{
    public override string ModuleName => "TestPlugin";
    public override string ModuleVersion => "1.0.0";

    private static PlayerCapability<ISessionsPlayer> CapabilityPlayer { get; } = new("sessions:player");
    private static PluginCapability<ISessionsServer> CapabilityServer { get; } = new("sessions:server");

    public override void Load(bool isReload)
    {
        var server = CapabilityServer.Get()!.Server;

        if (server != null)
            Logger.LogInformation($"Server: {server.Id} (${server.Ip}:${server.Port}) - Map: ${server.Map!.Id}");

        AddCommand("css_test", "test", CommandTest);
        RegisterEventHandler<EventPlayerConnect>(EventConnect, HookMode.Pre);
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

        Logger.LogInformation($"Player: {player.Id} - Session: {session.Id} - Server: {server.Id}/{CounterStrikeSharp.API.Server.MapName}[{server.Map!.Id}] ({server.Ip}:{server.Port}");
    }

    private HookResult EventConnect(EventPlayerConnect @event, GameEventInfo info)
    {
        if (@event.Userid == null)
            return HookResult.Continue;

        var controller = @event.Userid;
        var player = CapabilityPlayer.Get(controller)!.Player;

        if (player == null)
            return HookResult.Continue;

        Logger.LogInformation($"Player {player.Id} connected");
        return HookResult.Continue;
    }
}