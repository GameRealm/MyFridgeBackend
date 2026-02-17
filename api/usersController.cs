using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using myFridge.DTOs.Users;
using myFridge.Services.Interfaces;
using System.Text.Json;

namespace myFridge.api.Controllers;
// On the future
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
}