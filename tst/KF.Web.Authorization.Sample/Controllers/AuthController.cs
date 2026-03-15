using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KF.Web.Authorization.Sample.Models;
using KF.Web.Authorization.Sample.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace KF.Web.Authorization.Sample.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IOptions<JwtOptions> jwtOptions, ILogger<AuthController> logger)
    {
        _jwtOptions = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("token")]
    public IActionResult CreateToken([FromBody] TokenRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.UserName))
        {
            return BadRequest("UserName is required.");
        }

        if (string.IsNullOrWhiteSpace(_jwtOptions.Key))
        {
            _logger.LogError("JWT key is missing. Configure Jwt:Key in appsettings.");
            return StatusCode(StatusCodes.Status500InternalServerError, "JWT configuration missing.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.UserName.Trim()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in request.Roles.Where(r => !string.IsNullOrWhiteSpace(r)))
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
        }

        foreach (var kvp in request.AdditionalClaims)
        {
            if (!string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
            {
                claims.Add(new Claim(kvp.Key.Trim(), kvp.Value.Trim()));
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        var handler = new JwtSecurityTokenHandler();
        var tokenValue = handler.WriteToken(token);

        return Ok(new { access_token = tokenValue, expires_in = 3600 });
    }
}
