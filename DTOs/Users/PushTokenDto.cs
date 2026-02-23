using System.Text.Json.Serialization;

namespace myFridge.DTOs.Users;

public class PushTokenDto
{
    [JsonPropertyName("id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("push_token")]
    public string? PushToken { get; set; }
}

public class UpdateTokenDto
{
    public string? Token { get; set; }
}