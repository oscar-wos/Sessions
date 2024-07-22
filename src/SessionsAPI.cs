using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using Sessions.API;

namespace Sessions;

public partial class Sessions
{
    private static PlayerCapability<ISessionsPlayer> CapabilityPlayer { get; } = new("sessions:player");
    private static PluginCapability<ISessionsServer> CapabilityServer { get; } = new("sessions:server");
    private static PlayerCapability<PlayerConnectedEvent> CapabilityPlayerConnected { get; } = new("sessions:player_connected");

    private void RegisterCapabilities()
    {
        Capabilities.RegisterPlayerCapability(CapabilityPlayer, controller => new SessionsPlayer(controller, this));
        Capabilities.RegisterPluginCapability(CapabilityServer, () => new SessionsServer(this));
        Capabilities.RegisterPlayerCapability(CapabilityPlayerConnected, controller =>  new PlayerConnectedEvent(controller));
    }
}

public class SessionsPlayer(CCSPlayerController controller, Sessions plugin) : ISessionsPlayer
{
    public Player? Player => plugin.Players.TryGetValue(controller.Slot, out var value) ? value : null;
    public Session? Session => Player?.Session ?? null;
}

public class SessionsServer(Sessions plugin) : ISessionsServer
{
    public Server? Server => plugin.Server ?? null;
}

public class PlayerConnectedEvent(CCSPlayerController controller)
{
    public event EventHandler<Player>? PlayerConnected;

    public void TriggerEvent(CCSPlayerController controller, Player player)
    {
        PlayerConnected?.Invoke(this, player);
    }
}