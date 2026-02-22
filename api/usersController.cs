using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using myFridge.DTOs.Users;
using myFridge.Services.Interfaces;
using System.Text.Json;

namespace myFridge.api.Controllers;
[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (userId == null) return Unauthorized();

            var token = GetToken();

            var jsonResult = await _userService.GetUserProfileAsync(token, userId);

            return Ok(JsonSerializer.Deserialize<object>(jsonResult));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("profile")]
    public async Task<IActionResult> CreateProfile(UserDto dto)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (userId == null) return Unauthorized();

            var token = GetToken();

            var jsonResult = await _userService.CreateProfileAsync(token, userId, dto);

            return Ok(JsonSerializer.Deserialize<object>(jsonResult));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("profile")]
    public async Task<IActionResult> DeleteProfile()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (userId == null) return Unauthorized();

            var token = GetToken();

            var jsonResult = await _userService.DeleteProfileAsync(token, userId);

            return Ok(JsonSerializer.Deserialize<object>(jsonResult));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPatch("profile")] 
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserDto dto)
    {
        try
        {
            var token = GetToken();

            var resultJson = await _userService.UpdateUserAsync(token, dto);

            return Ok(JsonSerializer.Deserialize<object>(resultJson));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
    private string GetToken()
    {
        return Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
    }

    [HttpPost("update-push-token")]
    public async Task<IActionResult> UpdatePushToken([FromBody] PushTokenDto request)
    {
        // 1. Перевіряємо, чи не прийшов пустий Guid або пустий токен
        if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.PushToken))
        {
            return BadRequest(new { message = "UserId та PushToken є обов'язковими." });
        }

        // 2. Передаємо в сервіс
        // (Якщо твій метод у сервісі приймає Guid, передаємо як є. 
        // Якщо сервіс очікує string, додай .ToString(): request.UserId.ToString())
        await _userService.UpdateUserPushTokenAsync(request.UserId, request.PushToken);

        // 3. Кажемо мобілці "Все супер!"
        return Ok(new { message = "Push-токен успішно оновлено!" });
    }
}