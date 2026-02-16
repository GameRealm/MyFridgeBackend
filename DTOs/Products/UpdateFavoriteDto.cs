using System.Text.Json.Serialization;
namespace myFridge.DTOs.Products;

public class UpdateFavoriteDto
{
    [JsonPropertyName("is_favorite")]
    public bool Is_Favorite { get; set; }
}
