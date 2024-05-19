namespace Core;

public class PlayerSQL()
{
    public int Id { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    
    public SessionSQL? Session { get; set; }
}

public class ServerSQL()
{
    public int Id { get; set; }

    public MapSQL? Map { get; set; }
}

public class SessionSQL()
{
    public int Id { get; set; }
}

public class MapSQL()
{
    public short Id { get; set; }
}

public class Database
{
    
}