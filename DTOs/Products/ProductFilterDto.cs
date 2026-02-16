namespace myFridge.DTOs.Products;

public class ProductFilterDto
{
    public string? SearchTerm { get; set; }       // Пошук по назві
    public bool? IsFavorite { get; set; }         // Тільки улюблені?
    public Guid? StorageId { get; set; }          // Конкретне місце
    public int? ExpiringInDays { get; set; }      // Скільки днів до псування
    public string? ExpirationCategory { get; set; } // "soon", "medium", "later"
    public string SortBy { get; set; } = "created_at"; // Сортування
    public bool SortDescending { get; set; } = false;  // Напрямок сортування
}
