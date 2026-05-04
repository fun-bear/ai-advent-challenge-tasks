using MessagePack;

namespace AIAdventChallenge.Contracts;

[MessagePackObject]
public class OrderContract
{
    [Key(0)] public int OrderId { get; set; }
    [Key(1)] public string CustomerId { get; set; } = string.Empty;
    [Key(2)] public string TotalAmount { get; set; } = string.Empty;
    [Key(3)] public DateTime CreatedAt { get; set; }
    [Key(4)] public bool IsPaid { get; set; }
}

[MessagePackObject]
public class UserProfileContract
{
    [Key(0)] public int UserId { get; set; }
    [Key(1)] public string Username { get; set; } = string.Empty;
    [Key(2)] public string Email { get; set; } = string.Empty;
    [Key(3)] public bool IsActive { get; set; }
}

[MessagePackObject]
public class SensorReadingContract
{
    [Key(0)] public string DeviceId { get; set; } = string.Empty;
    [Key(1)] public float Temperature { get; set; }
    [Key(2)] public float Humidity { get; set; }
    [Key(3)] public long Timestamp { get; set; }
}
