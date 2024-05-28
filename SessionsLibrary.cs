/*using CounterStrikeSharp.API.Core;

namespace Sessions;


public class SessionsPlayer : ISessions
{
    private readonly PlayerSQL? _player;

    public SessionsPlayer(CCSPlayerController player)
    {
        _player = new PlayerSQL
        {
            Id = 1,
            FirstSeen = DateTime.Now,
            LastSeen = DateTime.Now,
            Session = new SessionSQL
            {
                Id = 1
            }
        };
    }

    public PlayerSQL? Player => _player;
}
*/