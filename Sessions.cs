using CounterStrikeSharp.API.Core;

namespace Sessions;

public partial class Sessions : BasePlugin, IPluginConfig<CoreConfig>
{
    public CoreConfig Config { get; set; } = new();

    public override string ModuleName => "Sessions";
    public override string ModuleDescription => "Track player sessions";
    public override string ModuleAuthor => "Oscar Wos-Szlaga";
    public override string ModuleVersion => "1.2.0";

    public required IDatabase _database;
    
    public void OnConfigParsed(CoreConfig config)
	{
        Config = config;

        _database = new DatabaseFactory(config).Database;
        _database.CreateTablesAsync();
    }
}