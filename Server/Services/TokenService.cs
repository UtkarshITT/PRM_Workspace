using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PRM.Server.Configuration;
using PRM.Server.Models.Entities;

namespace PRM.Server.Services;

public interface ITokenService
{
	(string Token, DateTime ExpiresAt) GenerateToken(User user, Employee? employee);
}

public class TokenService : ITokenService
{
	private readonly JwtSettings _settings;

	public TokenService(IOptions<JwtSettings> settings)
	{
		_settings = settings.Value;
	}

	public (string Token, DateTime ExpiresAt) GenerateToken(User user, Employee? employee)
	{
		var expiresAt = DateTime.UtcNow.AddHours(_settings.ExpiryHours);
		var claims = BuildClaims(user, employee);
		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
		var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: _settings.Issuer,
			audience: _settings.Audience,
			claims: claims,
			expires: expiresAt,
			signingCredentials: credentials);

		return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
	}

	private static IEnumerable<Claim> BuildClaims(User user, Employee? employee)
	{
		var claims = new List<Claim>
		{
			new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
			new(ClaimTypes.Role, user.Role),
			new(ClaimTypes.Name, user.FullName),
			new("force_password_change", user.ForcePasswordChange.ToString().ToLowerInvariant())
		};

		if (employee != null)
		{
			claims.Add(new Claim("employee_id", employee.Id.ToString()));

			if (user.Role == "MANAGER")
			{
				claims.Add(new Claim("manager_id", user.Id.ToString()));
			}
		}

		return claims;
	}
}
