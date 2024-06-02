using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
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
        AddCommand("css_test", "test", (controller, _) =>
        {
            if (controller == null)
                return;

            var server = CapabilityServer.Get()!.Server;
            var player = CapabilityPlayer.Get(controller)!.Player;
            var session = CapabilityPlayer.Get(controller)!.Session;
            
            Logger.LogInformation($"Server: {server!.Id} - Map: ${server!.Map!.Id} - Player: {player!.Id} - Session: {session!.Id}");
        });

        RegisterEventHandler<EventPlayerConnect>((@event, _) =>
        {
            if (@event.Userid == null)
                return HookResult.Continue;

            var controller = @event.Userid;
            var player = CapabilityPlayer.Get(controller)!.Player;

            Logger.LogInformation($"Player {player!.Id} connected");
            return HookResult.Continue;
        }, HookMode.Pre);
    }
}