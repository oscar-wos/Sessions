using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using Sessions.API;

namespace Sessions;

public partial class Sessions
{
    private static PlayerCapability<ISessionsPlayer> CapabilityPlayer { get; } = new("sessions:player");
    private static PluginCapability<ISessionsServer> CapabilityServer { get; } = new("sessions:server");

    private void RegisterCapabilities()
    {
        Capabilities.RegisterPlayerCapability(CapabilityPlayer, player => new SessionsPlayer(player, this));
        Capabilities.RegisterPluginCapability(CapabilityServer, () => new SessionsServer(this));
    }
}

public class SessionsPlayer(CCSPlayerController player, Sessions plugin) : ISessionsPlayer
{
    public Player? Player { get; } = plugin.Players.TryGetValue(player.Slot, out var value) ? value : null;
    public Session? Session => Player?.Session;
}

public class SessionsServer(Sessions plugin) : ISessionsServer
{
    public Server? Server => plugin.Server;
}