using Dapper;
using Npgsql;
using Microsoft.Extensions.Logging;
using Sessions.API;

namespace Sessions;

public class PostgresService : IDatabase
{
    private readonly ILogger _logger;
    private readonly PostgresServiceQueries _queries;
    private readonly NpgsqlConnection _connection;

    public PostgresService(SessionsConfig config, ILogger logger)
    {
        _logger = logger;
        _queries = new PostgresServiceQueries();
        var connectionString = BuildConnectionString(config);

        try
        {
            _connection = new NpgsqlConnection(connectionString);
            _connection.Open();
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Failed to connect to the database");
            throw;
        }
    }

    private static string BuildConnectionString(SessionsConfig config)
    {
        NpgsqlConnectionStringBuilder builder = new()
        {
            Host = config.DatabaseHost,
            Port = config.DatabasePort,
            Username = config.DatabaseUser,
            Password = config.DatabasePassword,
            Database = config.DatabaseName,
            Pooling = true
        };

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
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Failed to create tables");
            throw;
        }
    }

    public async Task<Server> GetServerAsync(string serverIp, ushort serverPort)
    {
        var serverPortSigned = (short)(serverPort - 0x8000);

        try
        {
            var result = await _connection.QueryFirstOrDefaultAsync<Server>(_queries.SelectServer, new { ServerIp = serverIp, ServerPort = serverPortSigned });

            if (result != null)
                return result;

            return await _connection.QuerySingleAsync<Server>(_queries.InsertServer, new { ServerIp = serverIp, ServerPort = serverPortSigned });
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Error while getting server");
            throw;
        }
    }

    public async Task<Map> GetMapAsync(string mapName)
    {
        try
        {
            var result = await _connection.QueryFirstOrDefaultAsync<Map>(_queries.SelectMap, new { MapName = mapName });

            if (result != null)
                return result;

            return await _connection.QuerySingleAsync<Map>(_queries.InsertMap, new { MapName = mapName });
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Error while getting map");
            throw;
        }
    }

    public async Task<Player> GetPlayerAsync(ulong steamId)
    {
        try
        {
            var result = await _connection.QueryFirstOrDefaultAsync<Player>(_queries.SelectPlayer, new { SteamId = (long)steamId });

            if (result != null)
                return result;

            return await _connection.QuerySingleAsync<Player>(_queries.InsertPlayer, new { SteamId = (long)steamId });
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Error while getting player");
            throw;
        }
    }

    public async Task<Session> GetSessionAsync(int playerId, int serverId, int mapId, string ip)
    {
        try
        {
            return await _connection.QuerySingleAsync<Session>(_queries.InsertSession, new { PlayerId = playerId, ServerId = serverId, MapId = mapId, Ip = ip });
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Error while getting session");
            throw;
        }
    }

    public async Task<Alias?> GetAliasAsync(int playerId)
    {
        try
        {
            return await _connection.QueryFirstOrDefaultAsync<Alias>(_queries.SelectAlias, new { PlayerId = playerId });
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Error while getting alias");
            throw;
        }
    }

    public async void InsertAlias(long sessionId, int playerId, string name)
    {
        try
        {
            NpgsqlCommand command = new(_queries.InsertAlias, _connection);

            command.Parameters.AddWithValue("@SessionId", sessionId);
            command.Parameters.AddWithValue("@PlayerId", playerId);
            command.Parameters.AddWithValue("@Name", name);

            await command.ExecuteNonQueryAsync();
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Error while inserting alias");
            throw;
        }
    }

    public async void InsertMessage(long sessionId, int playerId, MessageType messageType, string message)
    {
        try
        {
            NpgsqlCommand command = new(_queries.InsertMessage, _connection);

            command.Parameters.AddWithValue("@SessionId", sessionId);
            command.Parameters.AddWithValue("@PlayerId", playerId);
            command.Parameters.AddWithValue("@MessageType", (int)messageType);
            command.Parameters.AddWithValue("@Message", message);

            await command.ExecuteNonQueryAsync();
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Error while inserting message");
            throw;
        }
    }

    public async void UpdateSessions(List<int> playerIds, List<long> sessionIds)
    {
        await using var tx = await _connection.BeginTransactionAsync();

        try
        {
            foreach (var playerId in playerIds)
                await _connection.ExecuteAsync(_queries.UpdateSeen, new { PlayerId = playerId }, transaction: tx);

            foreach (var sessionId in sessionIds)
                await _connection.ExecuteAsync(_queries.UpdateSession, new { SessionId = sessionId }, transaction: tx);

            await tx.CommitAsync();
        }
        catch (NpgsqlException ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Error while updating sessions bulk");
            throw;
        }
    }

    public async void UpdateSeen(int playerId)
    {
        try
        {
            await _connection.ExecuteAsync(_queries.UpdateSeen, new { PlayerId = playerId });
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Error while updating seen");
            throw;
        }
    }
}

public class PostgresServiceQueries : LoadQueries, IDatabaseQueries
{
    protected override string CreateServers => """
                                               CREATE TABLE IF NOT EXISTS servers (
                                                       id SMALLSERIAL PRIMARY KEY,
                                                       server_ip INET NOT NULL,
                                                       server_port SMALLINT NOT NULL
                                                   )
                                               """;

    protected override string CreateMaps => """
                                            CREATE TABLE IF NOT EXISTS maps (
                                                    id SMALLSERIAL PRIMARY KEY,
                                                    map_name VARCHAR(256) NOT NULL
                                                )
                                            """;

    protected override string CreatePlayers => """
                                               CREATE TABLE IF NOT EXISTS players (
                                                       id SERIAL,
                                                       steam_id BIGINT NOT NULL PRIMARY KEY,
                                                       first_seen TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                                       last_seen TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
                                                   )
                                               """;

    protected override string CreateSessions => """
                                                CREATE TABLE IF NOT EXISTS sessions (
                                                        id BIGSERIAL PRIMARY KEY,
                                                        player_id INT NOT NULL,
                                                        server_id SMALLINT NOT NULL,
                                                        map_id SMALLINT NOT NULL,
                                                        ip INET NOT NULL,
                                                        start_time TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                                        end_time TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
                                                    )
                                                """;

    protected override string CreateAliases => """
                                               CREATE TABLE IF NOT EXISTS aliases (
                                                       id BIGSERIAL PRIMARY KEY,
                                                       session_id BIGINT NOT NULL,
                                                       player_id INT NOT NULL,
                                                       timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                                       name VARCHAR(128)
                                                   )
                                               """;

    protected override string CreateMessages => """
                                                CREATE TABLE IF NOT EXISTS messages (
                                                        id BIGSERIAL PRIMARY KEY,
                                                        session_id BIGINT NOT NULL,
                                                        player_id INT NOT NULL,
                                                        timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                                        message_type SMALLINT NOT NULL,
                                                        message VARCHAR(512)
                                                    )
                                                """;

    public string SelectServer => "SELECT id FROM servers WHERE server_ip = CAST(@ServerIp as INET) AND server_port = @ServerPort";
    public string SelectMap => "SELECT id FROM maps WHERE map_name = @MapName";
    public string SelectPlayer => "SELECT id FROM players WHERE steam_id = @SteamId";
    public string SelectAlias => "SELECT id, name FROM aliases WHERE player_id = @PlayerId ORDER BY id DESC LIMIT 1";
    public string InsertServer => "INSERT INTO servers (server_ip, server_port) VALUES (CAST(@ServerIp as INET), @ServerPort) RETURNING id";
    public string InsertMap => "INSERT INTO maps (map_name) VALUES (@MapName) RETURNING id";
    public string InsertPlayer => "INSERT INTO players (steam_id) VALUES (@SteamId) RETURNING id";
    public string InsertSession => "INSERT INTO sessions (player_id, server_id, map_id, ip) VALUES (@PlayerId, @ServerId, @MapId, CAST(@Ip as INET)) RETURNING id";
    public string InsertAlias => "INSERT INTO aliases (session_id, player_id, name) VALUES (@SessionId, @PlayerId, @Name)";
    public string InsertMessage => "INSERT INTO messages (session_id, player_id, message_type, message) VALUES (@SessionId, @PlayerId, @MessageType, @Message)";
    public string UpdateSession => "UPDATE sessions SET end_time = NOW() WHERE id = @SessionId";
    public string UpdateSeen => "UPDATE players SET last_seen = NOW() WHERE id = @PlayerId";
}