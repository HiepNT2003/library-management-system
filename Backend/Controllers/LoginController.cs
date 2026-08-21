using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Backend.Models;
using Backend.DTOs.Auth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/auth")]
public class LoginController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _config;

    public LoginController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration config, AppDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _config = config;
        _context = context;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(dto.Login);
        if (user == null)
        {
            user = await _userManager.FindByNameAsync(dto.Login);
        }

        if (user == null)
            return Unauthorized(new { message = "Invalid credentials" });
        
        if (user.ExpiredDate != null && user.ExpiredDate < DateTime.UtcNow)
        {
            return Unauthorized(new { message = "Account is expired" });
        }

        switch (user.Status)
        {
            case UserStatus.Inactive:
                return Unauthorized(new { message = "Account is inactive" });

            case UserStatus.Blocked:
                return Unauthorized(new { message = "Account is blocked" });
        }

        if (!user.EmailConfirmed)
            return Unauthorized(new { message = "Email not confirmed" });

        var result = await _signInManager
            .CheckPasswordSignInAsync(user, dto.Password, true);

        if (result.IsLockedOut)
            return Unauthorized(new { message = "Account locked" });

        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid username or password" });

        var roles = await _userManager.GetRolesAsync(user);

        var token = GenerateJwtToken(user, roles);

        var refreshToken = Guid.NewGuid().ToString();

        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            Expires = DateTime.UtcNow.AddDays(7),
            UserId = user.Id,
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        Response.Cookies.Append("refreshToken", refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(7)
            });
        
        var studentProfile = await _context.StudentProfiles
            .FirstOrDefaultAsync(x => x.UserId == user.Id);

        var staffProfile = await _context.StaffProfiles
            .FirstOrDefaultAsync(x => x.UserId == user.Id);

        var userCode = studentProfile?.StudentCode ?? staffProfile?.StaffCode;
        
        return Ok(new
        {
            accessToken = token,
            user = new
            {
                id = user.Id,
                userName = user.FullName ?? user.UserName,
                email = user.Email,
                roles,
                status = user.Status,
                UserCode = userCode
            }
        });
    }

    private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? ""),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var jwtKey = _config["Jwt:Key"] 
            ?? throw new Exception("Jwt:Key is missing");

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(3),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refreshToken"];

        if (refreshToken == null)
            return Unauthorized();

        var tokenEntity = await _context.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == refreshToken);

        if (tokenEntity == null || tokenEntity.IsRevoked || tokenEntity.Expires < DateTime.UtcNow)
            return Unauthorized();

        var user = tokenEntity.User;

        if (user.Status != UserStatus.Active)
        {
            return Unauthorized(new
            {
                message = user.Status switch
                {
                    UserStatus.Inactive => "Account is inactive",
                    UserStatus.Blocked => "Account is blocked",
                    _ => "Account is not allowed"
                }
            });
        }
        if (user.ExpiredDate != null && user.ExpiredDate < DateTime.UtcNow)
        {
            return BadRequest("Account is expired");
        }

        tokenEntity.IsRevoked = true;

        var newRefreshToken = Convert.ToBase64String(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));

        var newRefreshTokenEntity = new RefreshToken
        {
            Token = newRefreshToken,
            Expires = DateTime.UtcNow.AddDays(7),
            UserId = tokenEntity.UserId
        };

        _context.RefreshTokens.Add(newRefreshTokenEntity);
        await _context.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = GenerateJwtToken(user, roles);

        Response.Cookies.Append("refreshToken", newRefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(7)
            });

        return Ok(new { accessToken = newAccessToken });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];

        if (refreshToken != null)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(x => x.Token == refreshToken);

            if (token != null)
            {
                token.IsRevoked = true;
                await _context.SaveChangesAsync();
            }
        }

        Response.Cookies.Delete("refreshToken");

        return Ok();
    }

    // POST /api/auth/forgot-password
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return BadRequest(new { message = "Email không tồn tại trong hệ thống" });

        var studentProfile = await _context.StudentProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id);
        var staffProfile = await _context.StaffProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id);

        var code = studentProfile?.StudentCode ?? staffProfile?.StaffCode;
        if (code == null)
            return BadRequest(new { message = "Không tìm thấy mã xác nhận" });

        // Check verification code
        if (dto.VerifyCode != code)
            return BadRequest(new { message = "Mã sinh viên/cán bộ không đúng" });

        // Reset password = mã + @Utc1
        var newPassword = $"{code}@Utc1";
        await _userManager.RemovePasswordAsync(user);
        await _userManager.AddPasswordAsync(user, newPassword);

        return Ok(new { message = "Đặt lại mật khẩu thành công" });
    }
}

public class ForgotPasswordDto
{
    public string Email      { get; set; } = "";
    public string VerifyCode { get; set; } = "";
}