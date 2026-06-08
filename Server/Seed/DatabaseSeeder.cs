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
		await SeedDemoProjectsIfNeededAsync(context);
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

	private static async Task SeedDemoProjectsIfNeededAsync(PrmDbContext context)
	{
		if (await context.Projects.AnyAsync())
		{
			return;
		}

		var managers = await context.Users
			.Where(user => user.Role == Roles.Manager)
			.OrderBy(user => user.Id)
			.ToListAsync();

		if (managers.Count == 0)
		{
			return;
		}

		var now = DateTime.UtcNow;
		var projectDefinitions = new[]
		{
			("Alpha Portal", "Customer web portal", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), ProjectStatuses.Active, 120, 0),
			("Beta CRM", "CRM modernization", new DateOnly(2026, 2, 1), new DateOnly(2026, 8, 15), ProjectStatuses.Active, 80, 0),
			("Gamma Rewrite", "Legacy rewrite", new DateOnly(2026, 2, 1), new DateOnly(2026, 7, 1), ProjectStatuses.Active, 60, 1),
			("Delta Migrate", "Cloud migration", new DateOnly(2026, 4, 1), new DateOnly(2026, 9, 30), ProjectStatuses.Planned, 100, 2)
		};

		var projects = new List<Project>();

		foreach (var (name, description, start, end, status, storyPoints, managerIndex) in projectDefinitions)
		{
			var managerId = managers[Math.Min(managerIndex, managers.Count - 1)].Id;
			var project = new Project
			{
				ProjectName = name,
				Description = description,
				StartDate = start,
				EndDate = end,
				ProjectStatus = status,
				HealthStatus = "GREEN",
				TotalStoryPoints = storyPoints,
				ManagerUserId = managerId,
				IsActive = true,
				CreatedAt = now,
				UpdatedAt = now
			};

			context.Projects.Add(project);
			await context.SaveChangesAsync();
			project.ProjectCode = $"PRJ-{project.Id:D6}";
			projects.Add(project);
		}

		await context.SaveChangesAsync();

		var milestoneDefinitions = new (int ProjectIndex, string Title, DateOnly DueDate, int StoryPoints, short SortOrder, string Status)[]
		{
			(0, "Design Complete", new DateOnly(2026, 4, 1), 20, 1, MilestoneStatuses.Done),
			(0, "Backend API", new DateOnly(2026, 4, 15), 40, 2, MilestoneStatuses.InProgress),
			(0, "Testing", new DateOnly(2026, 4, 30), 35, 3, MilestoneStatuses.NotStarted),
			(0, "Go Live", new DateOnly(2026, 5, 15), 25, 4, MilestoneStatuses.NotStarted),
			(1, "Requirements", new DateOnly(2026, 3, 15), 15, 1, MilestoneStatuses.Done),
			(1, "Implementation", new DateOnly(2026, 6, 1), 45, 2, MilestoneStatuses.InProgress),
			(2, "Architecture", new DateOnly(2026, 3, 1), 10, 1, MilestoneStatuses.Done)
		};

		foreach (var (projectIndex, title, dueDate, storyPoints, sortOrder, status) in milestoneDefinitions)
		{
			context.ProjectMilestones.Add(new ProjectMilestone
			{
				ProjectId = projects[projectIndex].Id,
				MilestoneTitle = title,
				DueDate = dueDate,
				StoryPoints = storyPoints,
				SortOrder = sortOrder,
				MilestoneStatus = status,
				CompletedAt = status == MilestoneStatuses.Done ? now : null,
				CreatedAt = now,
				UpdatedAt = now
			});
		}

		await context.SaveChangesAsync();
		await SeedDemoAllocationsAsync(context, projects);
	}

	private static async Task SeedDemoAllocationsAsync(PrmDbContext context, IReadOnlyList<Project> projects)
	{
		if (await context.ProjectAllocations.AnyAsync())
		{
			return;
		}

		var employees = await context.Employees
			.Include(employee => employee.User)
			.Where(employee => employee.User.Role == Roles.Employee && employee.IsActive)
			.ToListAsync();

		if (employees.Count == 0 || projects.Count < 3)
		{
			return;
		}

		var manager = await context.Users.FirstAsync(user => user.Role == Roles.Manager);
		var now = DateTime.UtcNow;

		var allocations = new (int EmployeeIndex, int ProjectIndex, decimal Percentage, DateOnly Start, DateOnly End)[]
		{
			(0, 0, 50, new DateOnly(2026, 3, 1), new DateOnly(2026, 6, 30)),
			(0, 1, 50, new DateOnly(2026, 4, 1), new DateOnly(2026, 7, 31)),
			(3, 0, 100, new DateOnly(2026, 3, 1), new DateOnly(2026, 6, 30)),
			(2, 2, 75, new DateOnly(2026, 2, 1), new DateOnly(2026, 7, 1))
		};

		foreach (var (employeeIndex, projectIndex, percentage, start, end) in allocations)
		{
			if (employeeIndex >= employees.Count || projectIndex >= projects.Count)
			{
				continue;
			}

			var employee = employees[employeeIndex];
			context.ProjectAllocations.Add(new ProjectAllocation
			{
				EmployeeId = employee.Id,
				ProjectId = projects[projectIndex].Id,
				AllocationPercentage = percentage,
				AllocationStartDate = start,
				AllocationEndDate = end,
				AllocationStatus = "ACTIVE",
				AllocatedByManagerId = manager.Id,
				CreatedAt = now,
				UpdatedAt = now
			});

			employee.EmploymentStatus = "ALLOCATED";
			employee.UpdatedAt = now;
		}

		await context.SaveChangesAsync();
	}
}
