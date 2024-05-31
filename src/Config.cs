using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace Sessions;

public class SessionsConfig : BasePluginConfig
{
    public override int Version { get; set; } = 2;
    [JsonPropertyName("DatabaseType")] public string DatabaseType { get; set; } = "mysql";
    [JsonPropertyName("DatabaseHost")] public string DatabaseHost { get; set; } = "";
    [JsonPropertyName("DatabasePort")] public int DatabasePort { get; set; } = 3306;
    [JsonPropertyName("DatabaseUser")] public string DatabaseUser { get; set; } = "";
    [JsonPropertyName("DatabasePassword")] public string DatabasePassword { get; set; } = "";
    [JsonPropertyName("DatabaseName")] public string DatabaseName { get; set; } = "";
    [JsonPropertyName("DatabaseSsl")] public bool DatabaseSsl { get; set; } = false;
    [JsonPropertyName("DatabaseKey")] public string DatabaseKey { get; set; } = "";
    [JsonPropertyName("DatabaseCert")] public string DatabaseCert { get; set; } = "";
    [JsonPropertyName("DatabaseCa")] public string DatabaseCa { get; set; } = "";
}