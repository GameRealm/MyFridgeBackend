namespace myFridge.DTOs.Products;

public class UpdateProductDto
{
    public string? Name { get; set; }
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public Guid? StoragePlaceId { get; set; }
}
