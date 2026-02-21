using myFridge.DTOs.StoragePlaces;
using myFridge.DTOs.Users;
using System.Text.Json.Serialization;

namespace myFridge.DTOs.Products;

public class ProductDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }

    [JsonPropertyName("expiration_date")]
    public DateTime? ExpirationDate { get; set; }

    [JsonPropertyName("users")]
    public UserDto? User { get; set; }
    [JsonPropertyName("storage_places")]
    public StorageDto? StoragePlace { get; set; }
}