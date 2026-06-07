using Microsoft.EntityFrameworkCore;
using PRM.Server.Data;
using PRM.Server.Models.Entities;

namespace PRM.Server.Seed;

/// <summary>
/// Runtime bootstrap: first admin user only (BCrypt hash computed at startup).
/// Static reference data lives in EF migration <c>SeedReferenceData</c>.
/// </summary>
public static class DatabaseSeeder
{
	public static async Task SeedAsync(PrmDbContext context)
	{
		await context.Database.MigrateAsync();

		if (await context.Users.AnyAsync())
		{
			return;
		}

		var now = DateTime.UtcNow;

		context.Users.Add(new User
		{
			Username = "admin",
			Email = "admin@techserve.com",
			FullName = "System Administrator",
			PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234"),
			Role = "ADMIN",
			IsActive = true,
			ForcePasswordChange = true,
			CreatedAt = now,
			UpdatedAt = now
		});

		await context.SaveChangesAsync();
	}
}
