using Microsoft.Extensions.Logging;
using Sessions.API;

namespace Sessions;

public interface IDatabase
{
    string BuildConnectionString(SessionsConfig config);
    Task CreateTablesAsync();

    Task<ServerSQL> GetServerAsync(string serverIp, ushort serverPort);
    Task<MapSQL> GetMapAsync(string mapName);
    Task<PlayerSQL> GetPlayerAsync(ulong steamId);
    Task<SessionSQL> GetSessionAsync(int playerId, int serverId, int mapId, string ip);
    Task<AliasSQL?> GetAliasAsync(int playerId);

    void UpdateSeen(int playerId);
    void UpdateSessions(List<int> playerIds, List<long> sessionIds);

    void InsertAlias(long sessionId, int playerId, string alias);
    void InsertMessage(long sessionId, int playerId, MessageType messageType, string message);
}

public interface IDatabaseFactory
{
    IDatabase Database { get; }
}

public class DatabaseFactory : IDatabaseFactory
{
    private readonly IDatabase _database;
    private readonly ILogger _logger;

    public DatabaseFactory(SessionsConfig config)
    {
        CheckConfig(config);

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<DatabaseFactory>();

        _database = config.DatabaseType switch
        {
            "postgresql" => new PostgreService(config, _logger),
            "mysql" => new SqlService(config, _logger),
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

    public IDatabase Database => _database;
}