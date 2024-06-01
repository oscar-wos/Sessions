namespace Sessions;

public interface IDatabaseQueries
{
    string SelectServer { get; }
    string SelectMap { get; }
    string SelectPlayer { get; }
    string SelectAlias { get; }
    string InsertServer { get; }
    string InsertMap { get; }
    string InsertPlayer { get; }
    string InsertSession { get; }
    string InsertAlias { get; }
    string InsertMessage { get; }
    string UpdateSession { get; }
    string UpdateSeen { get; }
}

public abstract class LoadQueries
{
    protected abstract string CreateServers { get; }
    protected abstract string CreateMaps { get; }
    protected abstract string CreatePlayers { get; }
    protected abstract string CreateSessions { get; }
    protected abstract string CreateAliases { get; }
    protected abstract string CreateMessages { get; }

    public IEnumerable<string> GetLoadQueries()
    {
        yield return CreateServers;
        yield return CreateMaps;
        yield return CreatePlayers;
        yield return CreateSessions;
        yield return CreateAliases;
        yield return CreateMessages;
    }
}