using Dapper;
using MySqlConnector;
using Microsoft.Extensions.Logging;

namespace Sessions;

public class SqlService : IDatabase
{
    private readonly ILogger _logger;
    private readonly SqlServiceQueries _queries;

    private readonly string _connectionString;
    private readonly MySqlConnection _connection;

    public SqlService(CoreConfig config, ILogger logger)
    {
        _logger = logger;
        _queries = new SqlServiceQueries();
        _connectionString = BuildConnectionString(config);

        try
        {
            _connection = new MySqlConnection(_connectionString);
            _connection.Open();
        }
        catch (MySqlException ex)
        {
            _logger.LogError(ex, "Failed to connect to the database");
            throw;
        }
    }

    public string BuildConnectionString(CoreConfig config)
    {
        MySqlConnectionStringBuilder builder = new()
        {
            Server = config.DatabaseHost,
            Port = (uint)config.DatabasePort,
            UserID = config.DatabaseUser,
            Password = config.DatabasePassword,
            Database = config.DatabaseName,
            Keepalive = (uint)config.DatabaseKeepAlive,
            AllowUserVariables = true,
            Pooling = true,
        };

        return builder.ConnectionString;
    }

    public async Task<ServerSQL> GetServerAsync(string serverIp, ushort serverPort)
    {
        try
        {
            ServerSQL? result = await _connection.QueryFirstOrDefaultAsync<ServerSQL>(_queries.SelectServer, new { ServerIp = serverIp, ServerPort = serverPort });

            if (result != null)
                return result;

            await _connection.ExecuteAsync(_queries.InsertServer, new { ServerIp = serverIp, ServerPort = serverPort });
            return await GetServerAsync(serverIp, serverPort);
        }
        catch (MySqlException ex)
        {
            _logger.LogError(ex, "Error while getting server");
            throw;
        }
    }

    public async Task<MapSQL> GetMapAsync(string mapName)
    {
        try
        {
            MapSQL? result = await _connection.QueryFirstOrDefaultAsync<MapSQL>(_queries.SelectMap, new { MapName = mapName });

            if (result != null)
                return result;

            await _connection.ExecuteAsync(_queries.InsertMap, new { MapName = mapName });
            return await GetMapAsync(mapName);
        }
        catch (MySqlException ex)
        {
            _logger.LogError(ex, "Error while getting map");
            throw;
        }
    }

    public async Task<PlayerSQL> GetPlayerAsync(ulong steamId)
    {
        try
        {
            PlayerSQL? result = await _connection.QueryFirstOrDefaultAsync<PlayerSQL>(_queries.SelectPlayer, new { SteamId = steamId });

            if (result != null)
                return result;

            await _connection.ExecuteAsync(_queries.InsertPlayer, new { SteamId = steamId });
            return await GetPlayerAsync(steamId);
        }
        catch (MySqlException ex)
        {
            _logger.LogError(ex, "Error while getting player");
            throw;
        }
    }

    public async Task<SessionSQL> GetSessionAsync(int playerId, int serverId, int mapId, string ip)
    {
        try
        {
            var result = await _connection.ExecuteScalarAsync(_queries.InsertSession, new { PlayerId = playerId, ServerId = serverId, MapId = mapId, Ip = ip });
            return new SessionSQL { Id = Convert.ToInt32(result) };
        }
        catch (MySqlException ex)
        {
            _logger.LogError(ex, "Error while getting session");
            throw;
        }
    }

    public async void CreateTablesAsync()
    {
        try
        {
            await using MySqlTransaction tx = await _connection.BeginTransactionAsync();

            foreach (string query in _queries.GetCreateQueries())
                await _connection.ExecuteAsync(query, transaction: tx);

            await tx.CommitAsync();
        }
        catch (MySqlException ex)
        {
            _logger.LogError(ex, "Failed to create tables");
            throw;
        }
    }

    public async void UpdateSessionsBulkAsync(int[] sessionIds)
    {
        await using MySqlTransaction tx = await _connection.BeginTransactionAsync();

        try
        {
            foreach (int sessionId in sessionIds)
                _connection.Execute(_queries.UpdateSession, new { SessionId = sessionId }, transaction: tx);
            
            await tx.CommitAsync();
        }
        catch (MySqlException ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Error while updating sessions");
            throw;
        }
    }

    public void UpdateSeenAsync(int playerId) {
        try
        {
            _connection.Execute(_queries.UpdateSeen, new { PlayerId = playerId });
        }
        catch (MySqlException ex)
        {
            _logger.LogError(ex, "Error while updating seen");
            throw;
        }
    }
}

public class SqlServiceQueries : Queries
{
    public override string CreateServers => @"CREATE TABLE IF NOT EXISTS servers (
        id TINYINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
        server_ip VARCHAR(15) NOT NULL,
        server_port SMALLINT UNSIGNED NOT NULL
    )";

    public override string CreateMaps => @"CREATE TABLE IF NOT EXISTS maps (
        id SMALLINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
        map_name VARCHAR(32) NOT NULL
    )";

    public override string CreatePlayers => @"CREATE TABLE IF NOT EXISTS players (
        id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
        steam_id BIGINT UNSIGNED NOT NULL,
        first_seen DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
        last_seen DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
    )";

    public override string CreateSessions => @"CREATE TABLE IF NOT EXISTS sessions (
        id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
        player_id INT UNSIGNED NOT NULL,
        server_id TINYINT UNSIGNED NOT NULL,
        map_id SMALLINT UNSIGNED NOT NULL,
        ip VARCHAR(15) NOT NULL,
        start_time DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
        end_time DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
    )";

    public override string SelectServer => "SELECT id FROM servers WHERE server_ip = @ServerIp AND server_port = @ServerPort";
    public override string InsertServer => "INSERT INTO servers (server_ip, server_port) VALUES (@ServerIp, @ServerPort)";

    public override string SelectMap => "SELECT id FROM maps WHERE map_name = @MapName";
    public override string InsertMap => "INSERT INTO maps (map_name) VALUES (@MapName)";

    public override string SelectPlayer => "SELECT id FROM players WHERE steam_id = @SteamId";
    public override string InsertPlayer => "INSERT INTO players (steam_id) VALUES (@SteamId)";

    public override string InsertSession => "INSERT INTO sessions (player_id, server_id, map_id, ip) VALUES (@PlayerId, @ServerId, @MapId, @Ip); SELECT last_insert_id()";
    public override string UpdateSession => "UPDATE sessions SET end_time = NOW() WHERE id = @SessionId";
    public override string UpdateSeen => "UPDATE players SET last_seen = NOW() WHERE id = @PlayerId";
}