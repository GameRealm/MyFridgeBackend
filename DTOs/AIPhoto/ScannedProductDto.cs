namespace myFridge.DTOs.AIPhoto;

public class ScannedProductDto
{
    public string Name { get; set; } = string.Empty;
    public string ExpiryDate { get; set; } = string.Empty;
    public int? Quantity { get; set; }
    public double? Volume { get; set; } 
    public string Unit { get; set; } = "pcs";
    public string Category { get; set; } = "Others";
}
