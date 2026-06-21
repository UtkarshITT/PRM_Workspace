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
	(string Token, DateTime ExpiresAt) GenerateToken(User user, ResourceProfile? resourceProfile);
}

public class TokenService : ITokenService
{
	private readonly JwtSettings _settings;

	public TokenService(IOptions<JwtSettings> settings)
	{
		_settings = settings.Value;
	}

	public (string Token, DateTime ExpiresAt) GenerateToken(User user, ResourceProfile? resourceProfile)
	{
		var expiresAt = DateTime.UtcNow.AddHours(_settings.ExpiryHours);
		var claims = BuildClaims(user, resourceProfile);
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

	private static IEnumerable<Claim> BuildClaims(User user, ResourceProfile? resourceProfile)
	{
		var claims = new List<Claim>
		{
			new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
			new(ClaimTypes.Role, user.Role),
			new(ClaimTypes.Name, user.FullName),
			new("force_password_change", user.ForcePasswordChange.ToString().ToLowerInvariant())
		};

		if (resourceProfile != null)
		{
			claims.Add(new Claim("resource_profile_id", resourceProfile.Id.ToString()));

			if (user.Role == "MANAGER")
			{
				claims.Add(new Claim("manager_id", user.Id.ToString()));
			}
		}

		return claims;
	}
}
