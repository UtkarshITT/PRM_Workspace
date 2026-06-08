using Microsoft.EntityFrameworkCore;
using PRM.Server.Constants;
using PRM.Server.Data;
using PRM.Server.Models.Entities;

namespace PRM.Server.Seed;

/// <summary>
/// Runtime bootstrap: admin user + demo managers/employees for later phases.
/// Static reference data lives in EF migration <c>SeedReferenceData</c>.
/// </summary>
public static class DatabaseSeeder
{
	private const string DefaultPassword = "TempPass1";

	public static async Task SeedAsync(PrmDbContext context)
	{
		await context.Database.MigrateAsync();

		if (!await context.Users.AnyAsync())
		{
			await SeedAdminAsync(context);
		}

		await EnsureAdminEmployeeAsync(context);
		await SeedDemoDataIfNeededAsync(context);
	}

	private static async Task SeedAdminAsync(PrmDbContext context)
	{
		var now = DateTime.UtcNow;

		context.Users.Add(new User
		{
			Username = "admin",
			Email = "admin@techserve.com",
			FullName = "System Administrator",
			PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234"),
			Role = Roles.Admin,
			IsActive = true,
			ForcePasswordChange = true,
			CreatedAt = now,
			UpdatedAt = now
		});

		await context.SaveChangesAsync();
	}

	private static async Task EnsureAdminEmployeeAsync(PrmDbContext context)
	{
		var admin = await context.Users.FirstOrDefaultAsync(user => user.Username == "admin");
		if (admin == null)
		{
			return;
		}

		var hasEmployee = await context.Employees.AnyAsync(employee => employee.UserId == admin.Id);
		if (hasEmployee)
		{
			return;
		}

		var now = DateTime.UtcNow;
		context.Employees.Add(new Employee
		{
			UserId = admin.Id,
			EmployeeCode = $"EMP-{admin.Id:D6}",
			Department = "IT",
			Designation = "System Administrator",
			EmploymentStatus = "BENCH",
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		});

		await context.SaveChangesAsync();
	}

	private static async Task SeedDemoDataIfNeededAsync(PrmDbContext context)
	{
		var managerCount = await context.Users.CountAsync(user => user.Role == Roles.Manager);
		if (managerCount >= 2)
		{
			return;
		}

		var now = DateTime.UtcNow;
		var managers = new List<User>();
		var managerProfiles = new[]
		{
			("ankit.shah", "ankit.shah@techserve.com", "Ankit Shah"),
			("rohan.verma", "rohan.verma@techserve.com", "Rohan Verma")
		};

		foreach (var (username, email, fullName) in managerProfiles)
		{
			if (await context.Users.AnyAsync(user => user.Username == username))
			{
				continue;
			}

			var manager = new User
			{
				Username = username,
				Email = email,
				FullName = fullName,
				PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword),
				Role = Roles.Manager,
				IsActive = true,
				ForcePasswordChange = false,
				CreatedAt = now,
				UpdatedAt = now
			};

			context.Users.Add(manager);
			await context.SaveChangesAsync();

			context.Employees.Add(new Employee
			{
				UserId = manager.Id,
				EmployeeCode = $"EMP-{manager.Id:D6}",
				Department = "Delivery",
				Designation = "Delivery Manager",
				EmploymentStatus = "BENCH",
				IsActive = true,
				CreatedAt = now,
				UpdatedAt = now
			});

			await context.SaveChangesAsync();
			managers.Add(manager);
		}

		managers = await context.Users
			.Where(user => user.Role == Roles.Manager)
			.OrderBy(user => user.Id)
			.ToListAsync();

		if (managers.Count == 0)
		{
			return;
		}

		var employeeProfiles = new[]
		{
			("ravi.kumar", "ravi.kumar@techserve.com", "Ravi Kumar", "Backend", "Senior Developer", 0),
			("priya.sharma", "priya.sharma@techserve.com", "Priya Sharma", "Frontend", "UI Developer", 0),
			("sara.khan", "sara.khan@techserve.com", "Sara Khan", "QA", "QA Engineer", 1),
			("dev.patel", "dev.patel@techserve.com", "Dev Patel", "Backend", "Developer", 1)
		};

		foreach (var (username, email, fullName, department, designation, managerIndex) in employeeProfiles)
		{
			if (await context.Users.AnyAsync(user => user.Username == username))
			{
				continue;
			}

			var employeeUser = new User
			{
				Username = username,
				Email = email,
				FullName = fullName,
				PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword),
				Role = Roles.Employee,
				IsActive = true,
				ForcePasswordChange = false,
				CreatedAt = now,
				UpdatedAt = now
			};

			context.Users.Add(employeeUser);
			await context.SaveChangesAsync();

			var managerId = managers[Math.Min(managerIndex, managers.Count - 1)].Id;

			context.Employees.Add(new Employee
			{
				UserId = employeeUser.Id,
				ManagerId = managerId,
				EmployeeCode = $"EMP-{employeeUser.Id:D6}",
				Department = department,
				Designation = designation,
				EmploymentStatus = "BENCH",
				IsActive = true,
				CreatedAt = now,
				UpdatedAt = now
			});

			await context.SaveChangesAsync();
		}

		await SeedDemoSkillsAsync(context);
	}

	private static async Task SeedDemoSkillsAsync(PrmDbContext context)
	{
		if (await context.EmployeeSkills.AnyAsync())
		{
			return;
		}

		var skillDefinitions = new (string Name, string Category)[]
		{
			("Java", SkillCategories.Backend),
			("Spring Boot", SkillCategories.Backend),
			("MySQL", SkillCategories.Backend),
			("React", SkillCategories.Frontend),
			("Selenium", SkillCategories.Qa)
		};

		var skills = new Dictionary<string, Skill>();
		var now = DateTime.UtcNow;

		foreach (var (name, category) in skillDefinitions)
		{
			var skill = await context.Skills.FirstOrDefaultAsync(existing => existing.SkillName == name);
			if (skill == null)
			{
				skill = new Skill
				{
					SkillName = name,
					Category = category,
					IsActive = true,
					CreatedAt = now
				};
				context.Skills.Add(skill);
				await context.SaveChangesAsync();
			}

			skills[name] = skill;
		}

		var assignments = new (string Username, string SkillName, string Proficiency)[]
		{
			("ravi.kumar", "Java", ProficiencyLevels.Intermediate),
			("ravi.kumar", "Spring Boot", ProficiencyLevels.Advanced),
			("ravi.kumar", "MySQL", ProficiencyLevels.Intermediate),
			("priya.sharma", "React", ProficiencyLevels.Advanced),
			("sara.khan", "Selenium", ProficiencyLevels.Intermediate),
			("dev.patel", "Java", ProficiencyLevels.Intermediate),
			("dev.patel", "Spring Boot", ProficiencyLevels.Intermediate)
		};

		foreach (var (username, skillName, proficiency) in assignments)
		{
			var employee = await context.Employees
				.Include(item => item.User)
				.FirstOrDefaultAsync(item => item.User.Username == username);

			if (employee == null)
			{
				continue;
			}

			var skill = skills[skillName];
			var exists = await context.EmployeeSkills.AnyAsync(
				item => item.EmployeeId == employee.Id && item.SkillId == skill.Id);

			if (exists)
			{
				continue;
			}

			context.EmployeeSkills.Add(new EmployeeSkill
			{
				EmployeeId = employee.Id,
				SkillId = skill.Id,
				ProficiencyLevel = proficiency,
				CreatedAt = now
			});
		}

		await context.SaveChangesAsync();
	}
}
