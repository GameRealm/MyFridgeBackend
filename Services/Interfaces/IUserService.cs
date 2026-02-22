using myFridge.DTOs.Users;
namespace myFridge.Services.Interfaces;

public interface IUserService
{
    Task<string> GetUserProfileAsync(string token, string userId);
    Task<string> CreateProfileAsync(string token, string userId, UserDto dto);
    Task<string> DeleteProfileAsync(string token, string userId);
    Task<string> UpdateUserAsync(string token, UpdateUserDto dto);
    Task UpdateUserPushTokenAsync(Guid userId, string pushToken);
}