namespace myFridge.DTOs.Products;

public class ProductFilterDto
{
    public string? SearchTerm { get; set; }      
    public bool? IsFavorite { get; set; }        
    public Guid? StorageId { get; set; }          
    public int? ExpiringInDays { get; set; }      
    public string? ExpirationCategory { get; set; } 
    public string SortBy { get; set; } = "created_at"; 
    public bool SortDescending { get; set; } = false;  
}
