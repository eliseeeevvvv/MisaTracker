
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MisaTracker.Core;
using MisaTracker.Core.ORM.Models;

namespace MisaTrackerr.Server.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserInfrastructure _userInfrastructure;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserInfrastructure userInfrastructure,
            IConfiguration configuration)
        {
            _userInfrastructure = userInfrastructure;
            _configuration = configuration;
        }

        public class RegisterRequest
        {
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class LoginRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    message = "Заполни все поля."
                });
            }

            if (request.Name.Trim().Length < 2 ||
                request.Name.Trim().Length > 50)
            {
                return BadRequest(new
                {
                    message = "Имя должно содержать от 2 до 50 символов."
                });
            }

            if (request.Password.Length < 8)
            {
                return BadRequest(new
                {
                    message = "Пароль должен содержать минимум 8 символов."
                });
            }

            try
            {
                var success = await _userInfrastructure.UserSingUp(
                    request.Name,
                    request.Email,
                    request.Password);

                if (!success)
                {
                    return Conflict(new
                    {
                        message = "Этот email уже зарегистрирован или данные некорректны."
                    });
                }

                return Ok(new
                {
                    message = "Регистрация успешна. Теперь войди в аккаунт."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "Ошибка сервера при регистрации."
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    message = "Введи email и пароль."
                });
            }

            try
            {
                var user = await _userInfrastructure.GetUserForLoginAsync(
                    request.Email,
                    request.Password);

                if (user == null)
                {
                    return Unauthorized(new
                    {
                        message = "Неверный email или пароль."
                    });
                }

                var token = CreateToken(user);

                return Ok(new
                {
                    token,
                    user = new
                    {
                        id = user.Id,
                        name = user.Name,
                        email = user.Email,
                        roleId = user.RoleId
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "Ошибка сервера при входе."
                });
            }
        }

        private string CreateToken(User user)
        {
            var key = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "JWT Key не настроен.");

            var issuer = _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException(
                    "JWT Issuer не настроен.");

            var audience = _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException(
                    "JWT Audience не настроен.");

            var claims = new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    user.Name ?? user.Email ?? "User"),

                new Claim(
                    ClaimTypes.Email,
                    user.Email ?? string.Empty),

                new Claim(
                    ClaimTypes.Role,
                    user.RoleId == 2 ? "Admin" : "User")
            };

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(3),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}
