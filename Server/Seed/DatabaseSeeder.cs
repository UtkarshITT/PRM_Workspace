using Microsoft.EntityFrameworkCore;
using PRM.Server.Data;
using PRM.Server.Models.Entities;

namespace PRM.Server.Seed;

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
		var adminUser = new User
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
		};

		context.Users.Add(adminUser);
		await context.SaveChangesAsync();

		context.Employees.Add(new Employee
		{
			UserId = adminUser.Id,
			EmployeeCode = $"EMP-{adminUser.Id:D6}",
			Department = "Operations",
			Designation = "System Administrator",
			EmploymentStatus = "BENCH",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		});

		context.SystemConfigurations.AddRange(
			new SystemConfiguration
			{
				ConfigKey = "llm_provider",
				ConfigValue = "Gemini",
				Description = "Active LLM provider: Gemini or Groq",
				UpdatedAt = now
			},
			new SystemConfiguration
			{
				ConfigKey = "llm_api_key",
				ConfigValue = string.Empty,
				Description = "Encrypted API key for active LLM provider",
				UpdatedAt = now
			},
			new SystemConfiguration
			{
				ConfigKey = "scheduler_interval_hours",
				ConfigValue = "4",
				Description = "Background scheduler interval in hours",
				UpdatedAt = now
			},
			new SystemConfiguration
			{
				ConfigKey = "max_weekly_hours",
				ConfigValue = "40",
				Description = "Maximum billable hours per employee per week",
				UpdatedAt = now
			});

		context.ActivityTags.AddRange(
			CreateTag("BACKEND_API", "Backend API Development", "Backend", now),
			CreateTag("MICROSERVICES", "Microservices / Architecture", "Backend", now),
			CreateTag("DATABASE", "Database Design & Queries", "Backend", now),
			CreateTag("WEBSOCKET", "WebSocket / Real-time Features", "Backend", now),
			CreateTag("FRONTEND", "Frontend Development", "Frontend", now),
			CreateTag("CODE_REVIEW", "Code Review / Mentoring", "General", now),
			CreateTag("BUG_FIX", "Bug Fixing", "General", now),
			CreateTag("DEVOPS", "DevOps / Deployment", "DevOps", now),
			CreateTag("TESTING", "Testing & QA", "QA", now),
			CreateTag("DOCUMENTATION", "Documentation", "General", now),
			CreateTag("OTHER", "Other", "Other", now));

		await context.SaveChangesAsync();
	}

	private static ActivityTag CreateTag(string code, string name, string category, DateTime now)
	{
		return new ActivityTag
		{
			TagCode = code,
			TagName = name,
			TagCategory = category,
			IsActive = true,
			CreatedAt = now
		};
	}
}
