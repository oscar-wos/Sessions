using Dapper;
using Npgsql;
using Microsoft.Extensions.Logging;

namespace Sessions;

public class PostgreService : IDatabase
{
    private readonly ILogger _logger;
    private readonly PostgreServiceQueries _queries;

    private readonly string _connectionString;
    private readonly NpgsqlConnection _connection;

    public PostgreService(CoreConfig config, ILogger logger)
    {
        _logger = logger;
        _queries = new PostgreServiceQueries();
        _connectionString = BuildConnectionString(config);
        
        try
        {
            _connection = new NpgsqlConnection(_connectionString);
            _connection.Open();
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Failed to connect to the database");
            throw;
        }
    }

    public string BuildConnectionString(CoreConfig config)
    {
        NpgsqlConnectionStringBuilder builder = new()
        {
            Host = config.DatabaseHost,
            Port = config.DatabasePort,
            Username = config.DatabaseUser,
            Password = config.DatabasePassword,
            Database = config.DatabaseName,
            Pooling = true,
        };

        return builder.ConnectionString;
    }

    public async Task<ServerSQL> GetServerAsync(string serverIp, ushort serverPort)
    {
        short serverPortSigned = (short)(serverPort - 0x8000);

        try
        {
            ServerSQL? result = await _connection.QueryFirstOrDefaultAsync<ServerSQL>(_queries.SelectServer, new { ServerIp = serverIp, ServerPort = serverPortSigned });

            if (result != null)
                return result;

            return await _connection.QuerySingleAsync<ServerSQL>(_queries.InsertServer, new { ServerIp = serverIp, ServerPort = serverPortSigned });
        }
        catch (NpgsqlException ex)
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

            return await _connection.QuerySingleAsync<MapSQL>(_queries.InsertMap, new { MapName = mapName });
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Error while getting map");
            throw;
        }
    }

    public async Task<PlayerSQL> GetPlayerAsync(ulong steamId)
    {
        try
        {
            PlayerSQL? result = await _connection.QueryFirstOrDefaultAsync<PlayerSQL>(_queries.SelectPlayer, new { SteamId = (long)steamId });

            if (result != null)
                return result;

            return await _connection.QuerySingleAsync<PlayerSQL>(_queries.InsertPlayer, new { SteamId = (long)steamId });
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Error while getting player");
            throw;
        }
    }

    public async Task<SessionSQL> GetSessionAsync(int playerId, int serverId, int mapId, string ip)
    {
        try
        {
            return await _connection.QuerySingleAsync<SessionSQL>(_queries.InsertSession, new { PlayerId = playerId, ServerId = serverId, MapId = mapId, Ip = ip });
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Error while getting session");
            throw;
        }
    }

    public async Task<AliasSQL?> GetAliasAsync(int playerId)
    {
        try
        {
            return await _connection.QueryFirstOrDefaultAsync<AliasSQL>(_queries.SelectAlias, new { PlayerId = playerId });
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Error while getting alias");
            throw;
        }
    }

    public async void CreateTablesAsync()
    {
        try
        {
            await using NpgsqlTransaction tx = await _connection.BeginTransactionAsync();

            foreach (string query in _queries.GetCreateQueries())
                await _connection.ExecuteAsync(query, transaction: tx);

            await tx.CommitAsync();
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Failed to create tables");
            throw;
        }
    }

    public async void UpdateSessionsBulkAsync(int[] playerIds, long[] sessionIds)
    {
        await using NpgsqlTransaction tx = await _connection.BeginTransactionAsync();

        try
        {
            foreach (int playerId in playerIds)
                await _connection.ExecuteAsync(_queries.UpdateSeen, new { PlayerId = playerId }, transaction: tx);

            foreach (long sessionId in sessionIds)
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

    public void UpdateSeen(int playerId)
    {
        try
        {
            _connection.ExecuteAsync(_queries.UpdateSeen, new { PlayerId = playerId });
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Error while updating seen");
            throw;
        }
    }

    public void InsertAlias(long sessionId, int playerId, string alias)
    {
        try
        {
            NpgsqlCommand command = new(_queries.InsertAlias, _connection);

            command.Parameters.AddWithValue("@SessionId", sessionId);
            command.Parameters.AddWithValue("@PlayerId", playerId);
            command.Parameters.AddWithValue("@Alias", alias);
            
            command.ExecuteNonQuery();
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Error while inserting alias");
            throw;
        }
    }

    public void InsertMessage(long sessionId, int playerId, MessageType messageType, string message)
    {
        try
        {
            NpgsqlCommand command = new(_queries.InsertMessage, _connection);

            command.Parameters.AddWithValue("@SessionId", sessionId);
            command.Parameters.AddWithValue("@PlayerId", playerId);
            command.Parameters.AddWithValue("@MessageType", (int)messageType);
            command.Parameters.AddWithValue("@Message", message);
            
            command.ExecuteNonQuery();
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Error while inserting message");
            throw;
        }
    }
}

public class PostgreServiceQueries : Queries
{
    public override string CreateServers => @"CREATE TABLE IF NOT EXISTS servers (
        id SMALLSERIAL PRIMARY KEY,
        server_ip INET NOT NULL,
        server_port SMALLINT NOT NULL
    )";

    public override string CreateMaps => @"CREATE TABLE IF NOT EXISTS maps (
        id SMALLSERIAL PRIMARY KEY,
        map_name VARCHAR(256) NOT NULL
    )";

    public override string CreatePlayers => @"CREATE TABLE IF NOT EXISTS players (
        id SERIAL,
        steam_id BIGINT NOT NULL PRIMARY KEY,
        first_seen TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
        last_seen TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
    )";

    public override string CreateSessions => @"CREATE TABLE IF NOT EXISTS sessions (
        id BIGSERIAL PRIMARY KEY,
        player_id INT NOT NULL,
        server_id SMALLINT NOT NULL,
        map_id SMALLINT NOT NULL,
        ip INET NOT NULL,
        start_time TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
        end_time TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
    )";

    public override string CreateAliases => @"CREATE TABLE IF NOT EXISTS aliases (
        id BIGSERIAL PRIMARY KEY,
        session_id BIGINT NOT NULL,
        player_id INT NOT NULL,
        timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
        alias VARCHAR(256)
    )";

    public override string CreateMessages => @"CREATE TABLE IF NOT EXISTS messages (
        id BIGSERIAL PRIMARY KEY,
        session_id BIGINT NOT NULL,
        player_id INT NOT NULL,
        timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
        message_type SMALLINT NOT NULL,
        message VARCHAR(512)
    )";

    public override string SelectServer => "SELECT id FROM servers WHERE server_ip = CAST(@ServerIp as INET) AND server_port = @ServerPort";
    public override string InsertServer => "INSERT INTO servers (server_ip, server_port) VALUES (CAST(@ServerIp as INET), @ServerPort) RETURNING id";

    public override string SelectMap => "SELECT id FROM maps WHERE map_name = @MapName";
    public override string InsertMap => "INSERT INTO maps (map_name) VALUES (@MapName) RETURNING id";

    public override string SelectPlayer => "SELECT id, first_seen, last_seen FROM players WHERE steam_id = @SteamId";
    public override string InsertPlayer => "INSERT INTO players (steam_id) VALUES (@SteamId) RETURNING id, first_seen, last_seen";

    public override string InsertSession => "INSERT INTO sessions (player_id, server_id, map_id, ip) VALUES (@PlayerId, @ServerId, @MapId, CAST(@Ip as INET)) RETURNING id";
    public override string UpdateSession => "UPDATE sessions SET end_time = NOW() WHERE id = @SessionId";
    public override string UpdateSeen => "UPDATE players SET last_seen = NOW() WHERE id = @PlayerId";

    public override string SelectAlias => "SELECT id, alias FROM aliases WHERE player_id = @PlayerId ORDER BY id DESC LIMIT 1";
    public override string InsertAlias => "INSERT INTO aliases (session_id, player_id, alias) VALUES (@SessionId, @PlayerId, @Alias)";
    public override string InsertMessage => "INSERT INTO messages (session_id, player_id, message_type, message) VALUES (@SessionId, @PlayerId, @MessageType, @Message)";
}