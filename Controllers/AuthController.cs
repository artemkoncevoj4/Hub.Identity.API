using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Identity.Database;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Runtime.CompilerServices;

namespace Identity.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IPasswordHasher hasher, IConfiguration config)
    {
        _db = db;
        _hasher = hasher;
        _config = config;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AuthRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Login == request.Login)) return BadRequest("Login is already taken");
        var user = new Users
        {
            Login = request.Login,
            PasswordHash = _hasher.Hash(request.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok("Registration successful");
        

    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(user => user.Login == request.Login);
        if (user == null || !_hasher.Verify(request.Password, user.PasswordHash)) return Unauthorized("Invalid login or password");

        var token = GenerateJwtToken(user);
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddHours(3)
        };
        Response.Cookies.Append("jwtToken", token, cookieOptions);
        return Ok (new
        {
            message = "Login successful",
            user = new UserDto(user.Id, user.Login)
        });

    }


    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("jwtToken");
        return Ok("Logged out successfully");
    }

    private string GenerateJwtToken(Users user)
    {
        var jwtKey = _config["JWT_KEY"];
        var jwtIssuer = _config["JWT_ISSUER"] ?? "FileVaultApi";
        var jwtAudience = _config["JWT_AUDIENCE"] ?? "FileVaultFront";

        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException("JWT_KEY is not configured!");
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creditals = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login)
        };

        var token = new JwtSecurityToken(
            jwtIssuer,
            jwtAudience,
            claims,
            expires: DateTime.Now.AddHours(3),
            signingCredentials: creditals
        );
        return new JwtSecurityTokenHandler().WriteToken(token);

    }
}
public record AuthRequest(string Login, string Password);
public record UserDto(int Id, string Login);