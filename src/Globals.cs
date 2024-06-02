using Sessions.API;

namespace Sessions;

public partial class Sessions
{
    public SessionsConfig Config { get; set; } = new();
    public override string ModuleName => "Sessions";
    public override string ModuleAuthor => "github.com/oscar-wos/Sessions";
    public override string ModuleVersion => "1.3.1";

    public Server? Server;
    private readonly Ip _ip = new();
    public required IDatabase Database;
    public readonly Dictionary<int, Player> Players = [];
}