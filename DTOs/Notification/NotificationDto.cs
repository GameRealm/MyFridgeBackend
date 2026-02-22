using System.Text.Json.Serialization;

namespace myFridge.DTOs.Notification;

public class NotificationProductDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("users")]
    public NotificationUserDto? User { get; set; }
}

public class NotificationUserDto
{
    [JsonPropertyName("push_token")]
    public string? PushToken { get; set; }
}