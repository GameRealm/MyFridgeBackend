namespace myFridge.DTOs.Products;
using System.Text.Json.Serialization;

public class CreateProductDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("expiration_date")]
    public DateTime? Expiration_Date { get; set; }

    [JsonPropertyName("storage_place_id")]
    public Guid Storage_Place_Id { get; set; }
    [JsonPropertyName("comments")]
    public string? Comment { get; set; }

}

