namespace myFridge.Services.Interfaces;

public interface IStoragePlaceService
{
    Task<string> GetMyStoragePlacesAsync(string token);
    Task<string> GetProductsInStoragePlaceAsync(string token, Guid storagePlaceId);
}