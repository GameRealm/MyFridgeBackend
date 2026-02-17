using Microsoft.AspNetCore.Mvc;
using myFridge.DTOs.Users;
using myFridge.Services.Interfaces; // Переконайтесь, що цей namespace правильний

namespace myFridge.api
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        // 🔥 ГОЛОВНА ЗМІНА: Ми інжектуємо СЕРВІС, а не HttpClient
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            try
            {
                var result = await _authService.LoginAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            try
            {
                // Тепер викликається логіка з AuthService, яка має права адміністратора
                var result = await _authService.RegisterAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}