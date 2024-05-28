using SessionsLibrary;

namespace Sessions;

public partial class Sessions
{
    public override string ModuleName => "Sessions";
    public override string ModuleAuthor => "github.com/oscar-wos/Sessions";
    public override string ModuleVersion => "1.3.0";

    public required IDatabase _database;
    public readonly Ip _ip = new();
    public ServerSQL? _server;
    public Dictionary<int, PlayerSQL> _players = [];
    public CounterStrikeSharp.API.Modules.Timers.Timer? _timer;
}

public enum MessageType : int
{
    Chat = 0,
    TeamChat = 1,
    Console = 2,
    Command = 3
}