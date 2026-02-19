using myFridge.DTOs.AIPhoto;

namespace myFridge.Services.Interfaces;

public interface IImageAnalysisService
{
    Task<List<ScannedProductDto>> AnalyzeProductImageAsync(IFormFile imageFile);
}
