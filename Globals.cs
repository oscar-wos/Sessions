using Sessions.API;

namespace Sessions;

public partial class Sessions
{
    public override string ModuleName => "Sessions";
    public override string ModuleAuthor => "github.com/oscar-wos/Sessions";
    public override string ModuleVersion => "1.3.0";

    public required IDatabase Database;
    public readonly Ip Ip = new();
    public Server? Server;
    public Dictionary<int, Player> Players = [];
    public CounterStrikeSharp.API.Modules.Timers.Timer? Timer;
}

public enum MessageType
{
    Chat = 0,
    TeamChat = 1,
    Console = 2,
    Command = 3
}