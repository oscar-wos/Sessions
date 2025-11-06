using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Npgsql;
using Sessions.API;

namespace Sessions;

public class SqlService : IDatabase
{
    private readonly ILogger _logger;
    private readonly SqlServiceQueries _queries;
    private readonly MySqlConnection _connection;

    public SqlService(SessionsConfig config, ILogger logger)
    {
        _logger = logger;
        _queries = new SqlServiceQueries();
        var connectionString = BuildConnectionString(config);

        try
        {
            _connection = new MySqlConnection(connectionString);
            _connection.Open();
        }
        catch (MySqlException ex)
        {
            _logger.LogError(ex, "Failed to connect to the database");
            throw;
        }
    }

    private static string BuildConnectionString(SessionsConfig config)
    {
        MySqlConnectionStringBuilder builder = new()
        {
            Server = config.DatabaseHost,
            Port = (uint)config.DatabasePort,
            UserID = config.DatabaseUser,
            Password = config.DatabasePassword,
            Database = config.DatabaseName,
            AllowUserVariables = true,
            Pooling = true,
        };

        if (!config.DatabaseSsl)
            return builder.ConnectionString;

        builder.SslMode = MySqlSslMode.Required;

        if (config.DatabaseCa.Length > 0)
        {
            builder.SslMode = MySqlSslMode.VerifyCA;
            builder.SslCa = config.DatabaseCa;
        }

        builder.SslKey = config.DatabaseKey;
        builder.SslCert = config.DatabaseCert;

        return builder.ConnectionString;
    }

    public async Task StartAsync()
    {
        try
        {
            await using var tx = await _connection.BeginTransactionAsync();

            foreach (var query in _queries.GetLoadQueries())
                await _connection.ExecuteAsync(query, transaction: tx);

            await tx.CommitAsync();
        }
        catch (MySqlException ex)
        {
            _logger.LogError(ex, "Failed to create tables");
            throw;
        }
    }

    public async Task<Server> GetServerAsync(string serverIp, ushort serverPort)
    {
        try
        {
            var result = await _connection.QueryFirstOrDefaultAsync<Server>(
                _queries.SelectServer,
                new { ServerIp = serverIp, ServerPort = serverPort }
            );

            if (result != null)
                return result;

            var insert = await _connection.ExecuteScalarAsync(
                _queries.InsertServer,
                new { ServerIp = serverIp, ServerPort = serverPort }
            );
            return new Server
            {
                Id = Convert.ToInt16(insert),
                Ip = serverIp,
                Port = serverPort,
            };
        }
        catch (MySqlException ex)
        {
            _logger.LogError(ex, "Error while getting server");
            throw;
        }
    }

    public async Task<Map> GetMapAsync(string mapName)
    {
        try
        {
            var result = await _connection.QueryFirstOrDefaultAsync<Map>(
                _queries.SelectMap,
                new { MapName = mapName }
            );

            if (result != null)
                return result;

            var insert = await _connection.ExecuteScalarAsync(
                _queries.InsertMap,
                new { MapName = mapName }
            );
            return new Map { Id = Convert.ToInt16(insert) };
        }
        catch (MySqlException ex)
        {
            _logger.LogError(ex, "Error while getting map");
            throw;
        }
    }

    public async Task<Player> GetPlayerAsync(ulong steamId)
    {
        try
        {
            var result = await _connection.QueryFirstOrDefaultAsync<Player>(
                _queries.SelectPlayer,
                new { SteamId = steamId }
            );

            if (result != null)
                return result;

            var insert = await _connection.ExecuteScalarAsync(
                _queries.InsertPlayer,
                new { SteamId = steamId }
            );
            return new Player { Id = Convert.ToInt32(insert) };
        }
        catch (MySqlException ex)
        {
            _logger.LogError(ex, "Error while getting player");
            throw;
        }
    }

    public async Task<Session> GetSessionAsync(int playerId, int serverId, int mapId, string ip)
    {
        try
        {
            var result = await _connection.ExecuteScalarAsync(
                _queries.InsertSession,
                new
                {
                    PlayerId = playerId,
                    ServerId = serverId,
                    MapId = mapId,
                    Ip = ip,
                }
            );
            return new Session { Id = Convert.ToInt64(result) };
        }
        catch (MySqlException ex)
        {
            _logger.LogError(ex, "Error while getting session");
            throw;
        }
    }

    public async Task<Alias?> GetAliasAsync(int playerId)
    {
        try
        {
            return await _connection.QueryFirstOrDefaultAsync<Alias>(
                _queries.SelectAlias,
                new { PlayerId = playerId }
            );
        }
        catch (MySqlException ex)
        {
            _logger.LogError(ex, "Error while getting alias");
            throw;
        }
    }

    public async void InsertAliasAsync(long sessionId, int playerId, string name)
    {
        try
        {
            MySqlCommand command = new(_queries.InsertAlias, _connection);

            command.Parameters.AddWithValue("@SessionId", sessionId);
            command.Parameters.AddWithValue("@PlayerId", playerId);
            command.Parameters.AddWithValue("@Name", name);

            await command.ExecuteNonQueryAsync();
        }
        catch (MySqlException ex)
        {
            _logger.LogError(ex, "Error while inserting alias");
            throw;
        }
    }

    public async void InsertMessageAsync(
        long sessionId,
        int playerId,
        MessageType messageType,
        string message
    )
    {
        try
        {
            MySqlCommand command = new(_queries.InsertMessage, _connection);

            command.Parameters.AddWithValue("@SessionId", sessionId);
            command.Parameters.AddWithValue("@PlayerId", playerId);
            command.Parameters.AddWithValue("@MessageType", (int)messageType);
            command.Parameters.AddWithValue("@Message", message);

            await command.ExecuteNonQueryAsync();
        }
        catch (MySqlException ex)
        {
            _logger.LogError(ex, "Error while inserting message");
            throw;
        }
    }

    public async void UpdateSessionsAsync(List<int> playerIds, List<long> sessionIds)
    {
        await using var tx = await _connection.BeginTransactionAsync();

        try
        {
            foreach (var playerId in playerIds)
                await _connection.ExecuteAsync(
                    _queries.UpdateSeen,
                    new { PlayerId = playerId },
                    transaction: tx
                );

            foreach (var sessionId in sessionIds)
                await _connection.ExecuteAsync(
                    _queries.UpdateSession,
                    new { SessionId = sessionId },
                    transaction: tx
                );

            await tx.CommitAsync();
        }
        catch (MySqlException ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Error while updating sessions");
            throw;
        }
    }

    public async void UpdateSeenAsync(int playerId)
    {
        try
        {
            await _connection.ExecuteAsync(_queries.UpdateSeen, new { PlayerId = playerId });
        }
        catch (MySqlException ex)
        {
            _logger.LogError(ex, "Error while updating seen");
            throw;
        }
    }
}

public class SqlServiceQueries : LoadQueries, IDatabaseQueries
{
    protected override string CreateServers =>
        """
            CREATE TABLE IF NOT EXISTS servers (
            id TINYINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
            server_ip VARCHAR(15) NOT NULL,
            server_port SMALLINT UNSIGNED NOT NULL
            )
            """;

    protected override string CreateMaps =>
        """
            CREATE TABLE IF NOT EXISTS maps (
            id SMALLINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
            map_name VARCHAR(64) NOT NULL
            )
            """;

    protected override string CreatePlayers =>
        """
            CREATE TABLE IF NOT EXISTS players (
            id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
            steam_id BIGINT UNSIGNED NOT NULL,
            first_seen DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            last_seen DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            )
            """;

    protected override string CreateSessions =>
        """
            CREATE TABLE IF NOT EXISTS sessions (
            id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
            player_id INT UNSIGNED NOT NULL,
            server_id TINYINT UNSIGNED NOT NULL,
            map_id SMALLINT UNSIGNED NOT NULL,
            ip VARCHAR(15) NOT NULL,
            start_time DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            end_time DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            )
            """;

    protected override string CreateAliases =>
        """
            CREATE TABLE IF NOT EXISTS aliases (
            id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
            session_id BIGINT UNSIGNED NOT NULL,
            player_id INT UNSIGNED NOT NULL,
            timestamp DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            name VARCHAR(32) COLLATE utf8mb4_unicode_520_ci
            )
            """;

    protected override string CreateMessages =>
        """
            CREATE TABLE IF NOT EXISTS messages (
            id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
            session_id BIGINT UNSIGNED NOT NULL,
            player_id INT UNSIGNED NOT NULL,
            timestamp DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            message_type TINYINT UNSIGNED NOT NULL,
            message VARCHAR(128) COLLATE utf8mb4_unicode_520_ci
            )
            """;

    public string SelectServer =>
        "SELECT id FROM servers WHERE server_ip = @ServerIp AND server_port = @ServerPort";
    public string SelectMap => "SELECT id FROM maps WHERE map_name = @MapName";
    public string SelectPlayer => "SELECT id FROM players WHERE steam_id = @SteamId";
    public string SelectAlias =>
        "SELECT id, name FROM aliases WHERE player_id = @PlayerId ORDER BY id DESC LIMIT 1";
    public string InsertServer =>
        "INSERT INTO servers (server_ip, server_port) VALUES (@ServerIp, @ServerPort); SELECT LAST_INSERT_ID()";
    public string InsertMap =>
        "INSERT INTO maps (map_name) VALUES (@MapName); SELECT LAST_INSERT_ID()";
    public string InsertPlayer =>
        "INSERT INTO players (steam_id) VALUES (@SteamId); SELECT LAST_INSERT_ID()";
    public string InsertSession =>
        "INSERT INTO sessions (player_id, server_id, map_id, ip) VALUES (@PlayerId, @ServerId, @MapId, @Ip); SELECT LAST_INSERT_ID()";
    public string InsertAlias =>
        "INSERT INTO aliases (session_id, player_id, name) VALUES (@SessionId, @PlayerId, @Name)";
    public string InsertMessage =>
        "INSERT INTO messages (session_id, player_id, message_type, message) VALUES (@SessionId, @PlayerId, @MessageType, @Message)";
    public string UpdateSession => "UPDATE sessions SET end_time = NOW() WHERE id = @SessionId";
    public string UpdateSeen => "UPDATE players SET last_seen = NOW() WHERE id = @PlayerId";
}
