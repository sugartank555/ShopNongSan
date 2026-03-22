using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ShopNongSan.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userMgr;
        private readonly SignInManager<IdentityUser> _signIn;
        private readonly IConfiguration _config;

        public AuthApiController(
            UserManager<IdentityUser> userMgr,
            SignInManager<IdentityUser> signIn,
            IConfiguration config)
        {
            _userMgr = userMgr;
            _signIn = signIn;
            _config = config;
        }

        // ─────────────────────────────────────────────────────────
        // POST api/auth/register
        // ─────────────────────────────────────────────────────────
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _userMgr.FindByEmailAsync(req.Email);
            if (existing != null)
                return Conflict(new { message = "Email này đã được đăng ký." });

            var user = new IdentityUser
            {
                UserName = req.Email,
                Email = req.Email
            };

            var result = await _userMgr.CreateAsync(user, req.Password);
            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            await _userMgr.AddToRoleAsync(user, "Customer");

            return Ok(new { message = "Đăng ký thành công." });
        }

        // ─────────────────────────────────────────────────────────
        // POST api/auth/login
        // ─────────────────────────────────────────────────────────
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userMgr.FindByEmailAsync(req.Email);
            if (user == null)
                return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });

            if (await _userMgr.IsLockedOutAsync(user))
                return Unauthorized(new { message = "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên." });

            var ok = await _signIn.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: false);
            if (!ok.Succeeded)
                return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });

            var roles = await _userMgr.GetRolesAsync(user);
            var token = GenerateJwt(user, roles);
            var expiresAt = DateTime.UtcNow.AddHours(8);

            return Ok(new
            {
                token,
                email = user.Email,
                userId = user.Id,
                roles,
                expiresAt
            });
        }

        // ─────────────────────────────────────────────────────────
        // GET api/auth/me  — thông tin user đang đăng nhập
        // ─────────────────────────────────────────────────────────
        [HttpGet("me")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userMgr.FindByIdAsync(userId!);
            if (user == null) return Unauthorized();

            var roles = await _userMgr.GetRolesAsync(user);
            return Ok(new
            {
                userId = user.Id,
                email = user.Email,
                userName = user.UserName,
                roles,
                isLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow
            });
        }

        // ─────────────────────────────────────────────────────────
        // Helper
        // ─────────────────────────────────────────────────────────
        private string GenerateJwt(IdentityUser user, IList<string> roles)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub,   user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email!),
                new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
                new(ClaimTypes.NameIdentifier,     user.Id),
                new(ClaimTypes.Email,              user.Email!),
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    // ── DTOs ─────────────────────────────────────────────────────
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        public string Password { get; set; } = string.Empty;
    }
}
