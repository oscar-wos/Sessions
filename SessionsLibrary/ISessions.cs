namespace SessionsLibrary;

public interface ISessions
{
    PlayerSQL? Player { get; }
}

public class ServerSQL
{
    public short Id { get; set; }

    public MapSQL? Map { get; set; }
}

public class MapSQL
{
    public short Id { get; set; }
}

public class PlayerSQL
{
    public int Id { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }

    public SessionSQL? Session { get; set; }
}

public class SessionSQL
{
    public long Id { get; set; }
}

public class AliasSQL
{
    public int Id { get; set; }
    public required string Alias { get; set; }
}
