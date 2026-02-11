namespace myFridge.DTOs.Products;

public class CreateProductDto
{
    public string? Name { get; set; }
    public int Quantity { get; set; }
    public string? Unit { get; set; }
    public DateTime? Expiration_Date { get; set; }
    public Guid Storage_Place_Id { get; set; }
    public Guid User_Id { get; set; }
}
