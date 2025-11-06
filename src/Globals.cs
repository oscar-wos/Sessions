using Sessions.API;

namespace Sessions;

public partial class Sessions
{
    public SessionsConfig Config { get; set; } = new();
    public override string ModuleName => "Sessions";
    public override string ModuleAuthor => "github.com/oscar-wos/Sessions";
    public override string ModuleVersion => "1.4.0";

    private readonly Ip _ip = new();
    private readonly Dictionary<int, Player> _players = [];

    public required IDatabase Database;
    public Server? Server { get; private set; }

    public IReadOnlyDictionary<int, Player> Players => _players;
}
