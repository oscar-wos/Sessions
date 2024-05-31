using Microsoft.Extensions.Logging;
using Sessions.API;

namespace Sessions;

public interface IDatabase
{
    Task CreateTablesAsync();

    Task<Server> GetServerAsync(string serverIp, ushort serverPort);
    Task<Map> GetMapAsync(string mapName);
    Task<Player> GetPlayerAsync(ulong steamId);
    Task<Session> GetSessionAsync(int playerId, int serverId, int mapId, string ip);
    Task<Alias?> GetAliasAsync(int playerId);

    void UpdateSeen(int playerId);
    void UpdateSessions(List<int> playerIds, List<long> sessionIds);

    void InsertAlias(long sessionId, int playerId, string name);
    void InsertMessage(long sessionId, int playerId, MessageType messageType, string message);
}

public class DatabaseFactory
{
    public DatabaseFactory(SessionsConfig config)
    {
        CheckConfig(config);

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        ILogger logger = loggerFactory.CreateLogger<DatabaseFactory>();

        Database = config.DatabaseType switch
        {
            "postgresql" => new PostgresService(config, logger),
            "mysql" => new SqlService(config, logger),
            _ => throw new InvalidOperationException("Database type is not supported"),
        };
    }

    private static void CheckConfig(SessionsConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.DatabaseType) || string.IsNullOrWhiteSpace(config.DatabaseHost)
            || string.IsNullOrWhiteSpace(config.DatabaseUser) || string.IsNullOrWhiteSpace(config.DatabaseName)
            || config.DatabasePort == 0)
            throw new InvalidOperationException("Database is not set in the configuration file");
    }

    public IDatabase Database { get; }
}