using myFridge.DTOs.StoragePlaces;
using myFridge.DTOs.Users;

namespace myFridge.DTOs.Products;

public class ProductDto
{
    public string? Name { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public UserDto? User { get; set; }
    public StorageDto? StoragePlace { get; set; }
}