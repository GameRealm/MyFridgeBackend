using System.Text.Json.Serialization;

namespace myFridge.DTOs.Users;

public class UserDto
{
    public Guid Id { get; set; }
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

