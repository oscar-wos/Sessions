using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using Sessions.API;

namespace Sessions;

public partial class Sessions
{
    public static PlayerCapability<ISessionsPlayer> CapabilityPlayer { get; } = new("sessions:player");

    private void RegisterCapabilities()
    {
        Capabilities.RegisterPlayerCapability(CapabilityPlayer, player => new SessionsPlayer(player, this));
    }
}

public class SessionsPlayer(CCSPlayerController player, Sessions plugin) : ISessionsPlayer
{
    public Player? Player { get; } = plugin.Players.TryGetValue(player.Slot, out var value) ? value : null;
    public Session? Session => Player?.Session;
}