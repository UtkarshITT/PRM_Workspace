using System.Security.Claims;
using PRM.Server.Exceptions;

namespace PRM.Server.Extensions;

public static class ClaimsPrincipalExtensions
{
	private const string ResourceProfileIdClaim = "resource_profile_id";

	public static long GetUserId(this ClaimsPrincipal user)
	{
		var claimValue = user.FindFirstValue(ClaimTypes.NameIdentifier)
			?? user.FindFirstValue("sub");

		return ParseRequiredLongClaim(claimValue, "user id");
	}

	public static long GetResourceProfileId(this ClaimsPrincipal user)
	{
		var claimValue = user.FindFirstValue(ResourceProfileIdClaim);
		return ParseRequiredLongClaim(claimValue, ResourceProfileIdClaim);
	}

	private static long ParseRequiredLongClaim(string? claimValue, string claimName)
	{
		if (!long.TryParse(claimValue, out var value))
		{
			throw new UnauthorizedAppException($"Required claim '{claimName}' is missing or invalid.");
		}

		return value;
	}
}
