using System.Text.Json.Serialization;

namespace myFridge.DTOs.StoragePlaces;

public class StorageDto
{
    public Guid Id { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}


