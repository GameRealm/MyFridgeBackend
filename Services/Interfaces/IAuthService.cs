using myFridge.DTOs.Users;
namespace myFridge.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto dto);

    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
}