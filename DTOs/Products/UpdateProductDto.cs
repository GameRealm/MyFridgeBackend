using System.Text.Json.Serialization;
namespace myFridge.DTOs.Products;

public class UpdateProductDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("quantity")]
    public decimal? Quantity { get; set; }

    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("expiration_date")]
    public DateTime? ExpirationDate { get; set; }

    [JsonPropertyName("storage_place_id")]
    public Guid? StoragePlaceId { get; set; }

    [JsonPropertyName("comments")]
    public string? Comment { get; set; }
}
