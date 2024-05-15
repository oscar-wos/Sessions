using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;

namespace Core;

// [MinimumApiVersion(160)]
public partial class Core : BasePlugin, IPluginConfig<CoreConfig>
{
    public CoreConfig Config { get; set; } = new();
    //internal static DataBase? _dataBase;
    internal static DataBaseService? _dataBaseService;
    internal static PostgresService? _postgresService;

    public override string ModuleName => "Core";
    public override string ModuleDescription => "";
    public override string ModuleAuthor => "Oscar Wos-Szlaga";
    public override string ModuleVersion => "0.0.1";

    
    public async void OnConfigParsed(CoreConfig config)
	{
        _postgresService = new PostgresService(config);
        await _postgresService.InitConnectAsync();
        /*
        var con = new NpgsqlConnection(
            connectionString: "Server=127.0.0.1;Port=5432;User Id=cs2;Password=lol;Database=cs2db;");
        con.Open();

        using var cmd = new NpgsqlCommand();
        cmd.Connection = con;

        cmd.CommandText = $"DROP TABLE IF EXISTS teachers";
        await cmd.ExecuteNonQueryAsync();
        /*s
        _dataBaseService = new DataBaseService(config);
        _dataBaseService.TestAndCheckDataBaseTableAsync().GetAwaiter().GetResult(); 1
        */
        Config = config;
    }
    

    public override void Load(bool hotReload)
    {
    }
    
    [ConsoleCommand("css_hello", "Say hello in the player language")]
    public void OnCommandHello(CCSPlayerController? player, CommandInfo command)
    {
        if (player != null)
        {
            string playerName = player.PlayerName;
            var playerLanguage = player.GetLanguage();
            ulong playerSteamId = player.SteamID;
            string commandString = command.GetArg(0).ToLower();

            _ = CheckPlayerCommand(commandString, playerName, playerSteamId);

            
        }

        if (player != null)
            command.ReplyToCommand($"{Localizer["hello.player", player.PlayerName, player.GetLanguage()]}");
    }
}