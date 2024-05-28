using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using SessionsLibrary;

namespace Sessions;

public partial class Sessions
{
    public static PlayerCapability<ISessionsPlayer> Capability_Player { get; } = new("sessions:player");
}

public class SessionsPlayer(CCSPlayerController player, Sessions plugin) : ISessionsPlayer
{
    private readonly PlayerSQL? _player = plugin._players.TryGetValue(player.Slot, out PlayerSQL? value) ? value : null;

    public PlayerSQL? PlayerSQL => _player;
    public SessionSQL? SessionSQL => _player?.Session;
}