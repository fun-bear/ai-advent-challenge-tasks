using MessagePack;

namespace AIAdventChallenge.MCPServer.Stdio.Contracts;

[MessagePackObject]
public class OrderContract
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public string ClientId { get; set; } = string.Empty;
    [Key(2)] public double Amount { get; set; }
    [Key(3)] public DateTime CreationDate { get; set; }
}

[MessagePackObject]
public class UserProfileContract
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public string Login { get; set; } = string.Empty;
    [Key(2)] public string ContactEmail { get; set; } = string.Empty;
    [Key(3)] public bool Active { get; set; }
}

[MessagePackObject]
public class SensorReadingContract
{
    [Key(0)] public string StationId { get; set; } = string.Empty;
    [Key(1)] public float TempCelsius { get; set; }
    [Key(2)] public float HumidityPercent { get; set; }
    [Key(3)] public long UnixTimestamp { get; set; }
}
