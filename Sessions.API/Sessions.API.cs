﻿namespace Sessions.API;

public class Server
{
    public short Id { get; init; }
    public Map? Map { get; init; }
}

public class Map
{
    public short Id { get; init; }
}

public class Player
{
    public int Id { get; init; }
    public Session? Session { get; init; }
}

public class Session
{
    public long Id { get; init; }
}

public class Alias
{
    public int Id { get; init; }
    public required string Name { get; init; }
}