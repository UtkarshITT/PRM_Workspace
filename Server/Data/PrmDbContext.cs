using Microsoft.EntityFrameworkCore;
using PRM.Server.Models.Entities;

namespace PRM.Server.Data;

public class PrmDbContext : DbContext
{
	public PrmDbContext(DbContextOptions<PrmDbContext> options) : base(options)
	{
	}

	public DbSet<User> Users => Set<User>();
	public DbSet<Employee> Employees => Set<Employee>();
	public DbSet<Skill> Skills => Set<Skill>();
	public DbSet<EmployeeSkill> EmployeeSkills => Set<EmployeeSkill>();
	public DbSet<Project> Projects => Set<Project>();
	public DbSet<ProjectMilestone> ProjectMilestones => Set<ProjectMilestone>();
	public DbSet<ProjectAllocation> ProjectAllocations => Set<ProjectAllocation>();
	public DbSet<Timesheet> Timesheets => Set<Timesheet>();
	public DbSet<TimesheetLineItem> TimesheetLineItems => Set<TimesheetLineItem>();
	public DbSet<ActivityTag> ActivityTags => Set<ActivityTag>();
	public DbSet<TimesheetLineItemActivityTag> TimesheetLineItemActivityTags => Set<TimesheetLineItemActivityTag>();
	public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();
	public DbSet<AiRequestLog> AiRequestLogs => Set<AiRequestLog>();
	public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
	public DbSet<SchedulerJobLog> SchedulerJobLogs => Set<SchedulerJobLog>();

	protected override void OnModelCreating(ModelBuilder builder)
	{
		ConfigureUser(builder);
		ConfigureEmployee(builder);
		ConfigureSkills(builder);
		ConfigureProjects(builder);
		ConfigureAllocations(builder);
		ConfigureTimesheets(builder);
		ConfigureActivityTags(builder);
		ConfigureSystemConfig(builder);
		ConfigureLogs(builder);
		ConfigureIndexes(builder);
	}

	private static void ConfigureUser(ModelBuilder builder)
	{
		builder.Entity<User>(entity =>
		{
			entity.ToTable("USERS");
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Id).HasColumnName("id");
			entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(100);
			entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
			entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(200);
			entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(500);
			entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(20);
			entity.Property(e => e.IsActive).HasColumnName("is_active");
			entity.Property(e => e.ForcePasswordChange).HasColumnName("force_password_change");
			entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
			entity.Property(e => e.CreatedAt).HasColumnName("created_at");
			entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
			entity.HasIndex(e => e.Username).IsUnique();
			entity.HasIndex(e => e.Email).IsUnique();
		});
	}

	private static void ConfigureEmployee(ModelBuilder builder)
	{
		builder.Entity<Employee>(entity =>
		{
			entity.ToTable("EMPLOYEES");
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Id).HasColumnName("id");
			entity.Property(e => e.UserId).HasColumnName("user_id");
			entity.Property(e => e.ManagerId).HasColumnName("manager_id");
			entity.Property(e => e.EmployeeCode).HasColumnName("employee_code").HasMaxLength(50);
			entity.Property(e => e.Department).HasColumnName("department").HasMaxLength(100);
			entity.Property(e => e.Designation).HasColumnName("designation").HasMaxLength(100);
			entity.Property(e => e.EmploymentStatus).HasColumnName("employment_status").HasMaxLength(20);
			entity.Property(e => e.IsActive).HasColumnName("is_active");
			entity.Property(e => e.JoinedAt).HasColumnName("joined_at").HasColumnType("date");
			entity.Property(e => e.CreatedAt).HasColumnName("created_at");
			entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
			entity.HasIndex(e => e.EmployeeCode).IsUnique();
			entity.HasIndex(e => e.UserId).IsUnique();
			entity.HasOne(e => e.User).WithOne(u => u.Employee).HasForeignKey<Employee>(e => e.UserId);
			entity.HasOne(e => e.Manager).WithMany().HasForeignKey(e => e.ManagerId).OnDelete(DeleteBehavior.Restrict);
		});
	}

	private static void ConfigureSkills(ModelBuilder builder)
	{
		builder.Entity<Skill>(entity =>
		{
			entity.ToTable("SKILLS");
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Id).HasColumnName("id");
			entity.Property(e => e.SkillName).HasColumnName("skill_name").HasMaxLength(100);
			entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(50);
			entity.Property(e => e.IsActive).HasColumnName("is_active");
			entity.Property(e => e.CreatedAt).HasColumnName("created_at");
			entity.HasIndex(e => e.SkillName).IsUnique();
		});

		builder.Entity<EmployeeSkill>(entity =>
		{
			entity.ToTable("EMPLOYEE_SKILLS");
			entity.HasKey(e => new { e.EmployeeId, e.SkillId });
			entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
			entity.Property(e => e.SkillId).HasColumnName("skill_id");
			entity.Property(e => e.ProficiencyLevel).HasColumnName("proficiency_level").HasMaxLength(20);
			entity.Property(e => e.CreatedAt).HasColumnName("created_at");
			entity.HasOne(e => e.Employee).WithMany(e => e.EmployeeSkills).HasForeignKey(e => e.EmployeeId);
			entity.HasOne(e => e.Skill).WithMany(s => s.EmployeeSkills).HasForeignKey(e => e.SkillId);
		});
	}

	private static void ConfigureProjects(ModelBuilder builder)
	{
		builder.Entity<Project>(entity =>
		{
			entity.ToTable("PROJECTS");
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Id).HasColumnName("id");
			entity.Property(e => e.ProjectCode).HasColumnName("project_code").HasMaxLength(50);
			entity.Property(e => e.ProjectName).HasColumnName("project_name").HasMaxLength(200);
			entity.Property(e => e.Description).HasColumnName("description");
			entity.Property(e => e.StartDate).HasColumnName("start_date").HasColumnType("date");
			entity.Property(e => e.EndDate).HasColumnName("end_date").HasColumnType("date");
			entity.Property(e => e.ProjectStatus).HasColumnName("project_status").HasMaxLength(20);
			entity.Property(e => e.HealthStatus).HasColumnName("health_status").HasMaxLength(20);
			entity.Property(e => e.TotalStoryPoints).HasColumnName("total_story_points");
			entity.Property(e => e.ManagerUserId).HasColumnName("manager_user_id");
			entity.Property(e => e.IsActive).HasColumnName("is_active");
			entity.Property(e => e.CreatedAt).HasColumnName("created_at");
			entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
			entity.HasIndex(e => e.ProjectCode).IsUnique();
			entity.HasOne(e => e.ManagerUser).WithMany().HasForeignKey(e => e.ManagerUserId);
		});

		builder.Entity<ProjectMilestone>(entity =>
		{
			entity.ToTable("PROJECT_MILESTONES");
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Id).HasColumnName("id");
			entity.Property(e => e.ProjectId).HasColumnName("project_id");
			entity.Property(e => e.MilestoneTitle).HasColumnName("milestone_title").HasMaxLength(200);
			entity.Property(e => e.Description).HasColumnName("description");
			entity.Property(e => e.DueDate).HasColumnName("due_date").HasColumnType("date");
			entity.Property(e => e.MilestoneStatus).HasColumnName("milestone_status").HasMaxLength(20);
			entity.Property(e => e.StoryPoints).HasColumnName("story_points");
			entity.Property(e => e.SortOrder).HasColumnName("sort_order");
			entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
			entity.Property(e => e.CreatedAt).HasColumnName("created_at");
			entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
			entity.HasOne(e => e.Project).WithMany(p => p.Milestones).HasForeignKey(e => e.ProjectId);
		});
	}

	private static void ConfigureAllocations(ModelBuilder builder)
	{
		builder.Entity<ProjectAllocation>(entity =>
		{
			entity.ToTable("PROJECT_ALLOCATIONS");
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Id).HasColumnName("id");
			entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
			entity.Property(e => e.ProjectId).HasColumnName("project_id");
			entity.Property(e => e.AllocationPercentage).HasColumnName("allocation_percentage").HasPrecision(5, 2);
			entity.Property(e => e.AllocationStartDate).HasColumnName("allocation_start_date").HasColumnType("date");
			entity.Property(e => e.AllocationEndDate).HasColumnName("allocation_end_date").HasColumnType("date");
			entity.Property(e => e.AllocationStatus).HasColumnName("allocation_status").HasMaxLength(20);
			entity.Property(e => e.AllocatedByManagerId).HasColumnName("allocated_by_manager_id");
			entity.Property(e => e.CreatedAt).HasColumnName("created_at");
			entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
			entity.HasOne(e => e.Employee).WithMany(e => e.ProjectAllocations).HasForeignKey(e => e.EmployeeId);
			entity.HasOne(e => e.Project).WithMany(p => p.ProjectAllocations).HasForeignKey(e => e.ProjectId);
			entity.HasOne(e => e.AllocatedByManager).WithMany().HasForeignKey(e => e.AllocatedByManagerId);
		});
	}

	private static void ConfigureTimesheets(ModelBuilder builder)
	{
		builder.Entity<Timesheet>(entity =>
		{
			entity.ToTable("TIMESHEETS");
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Id).HasColumnName("id");
			entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
			entity.Property(e => e.WeekStartDate).HasColumnName("week_start_date").HasColumnType("date");
			entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
			entity.Property(e => e.TotalHours).HasColumnName("total_hours").HasPrecision(5, 2);
			entity.Property(e => e.Remarks).HasColumnName("remarks");
			entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at");
			entity.Property(e => e.CreatedAt).HasColumnName("created_at");
			entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
			entity.HasIndex(e => new { e.EmployeeId, e.WeekStartDate }).IsUnique();
			entity.HasOne(e => e.Employee).WithMany(e => e.Timesheets).HasForeignKey(e => e.EmployeeId);
		});

		builder.Entity<TimesheetLineItem>(entity =>
		{
			entity.ToTable("TIMESHEET_LINE_ITEMS");
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Id).HasColumnName("id");
			entity.Property(e => e.TimesheetId).HasColumnName("timesheet_id");
			entity.Property(e => e.ProjectId).HasColumnName("project_id");
			entity.Property(e => e.HoursLogged).HasColumnName("hours_logged").HasPrecision(5, 2);
			entity.Property(e => e.WorkNotes).HasColumnName("work_notes");
			entity.Property(e => e.WorkDate).HasColumnName("work_date").HasColumnType("date");
			entity.Property(e => e.CreatedAt).HasColumnName("created_at");
			entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
			entity.HasOne(e => e.Timesheet).WithMany(t => t.LineItems).HasForeignKey(e => e.TimesheetId);
			entity.HasOne(e => e.Project).WithMany().HasForeignKey(e => e.ProjectId);
		});
	}

	private static void ConfigureActivityTags(ModelBuilder builder)
	{
		builder.Entity<ActivityTag>(entity =>
		{
			entity.ToTable("ACTIVITY_TAGS");
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Id).HasColumnName("id");
			entity.Property(e => e.TagCode).HasColumnName("tag_code").HasMaxLength(50);
			entity.Property(e => e.TagName).HasColumnName("tag_name").HasMaxLength(100);
			entity.Property(e => e.TagCategory).HasColumnName("tag_category").HasMaxLength(50);
			entity.Property(e => e.IsActive).HasColumnName("is_active");
			entity.Property(e => e.CreatedAt).HasColumnName("created_at");
			entity.HasIndex(e => e.TagCode).IsUnique();
		});

		builder.Entity<TimesheetLineItemActivityTag>(entity =>
		{
			entity.ToTable("TIMESHEET_LINE_ITEM_ACTIVITY_TAGS");
			entity.HasKey(e => new { e.TimesheetLineItemId, e.ActivityTagId });
			entity.Property(e => e.TimesheetLineItemId).HasColumnName("timesheet_line_item_id");
			entity.Property(e => e.ActivityTagId).HasColumnName("activity_tag_id");
			entity.Property(e => e.CustomTagText).HasColumnName("custom_tag_text").HasMaxLength(200);
			entity.HasOne(e => e.TimesheetLineItem).WithMany(l => l.ActivityTags).HasForeignKey(e => e.TimesheetLineItemId);
			entity.HasOne(e => e.ActivityTag).WithMany(t => t.LineItemTags).HasForeignKey(e => e.ActivityTagId);
		});
	}

	private static void ConfigureSystemConfig(ModelBuilder builder)
	{
		builder.Entity<SystemConfiguration>(entity =>
		{
			entity.ToTable("SYSTEM_CONFIGURATIONS");
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Id).HasColumnName("id");
			entity.Property(e => e.ConfigKey).HasColumnName("config_key").HasMaxLength(100);
			entity.Property(e => e.ConfigValue).HasColumnName("config_value");
			entity.Property(e => e.Description).HasColumnName("description");
			entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
			entity.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");
			entity.HasIndex(e => e.ConfigKey).IsUnique();
			entity.HasOne(e => e.UpdatedByUser).WithMany().HasForeignKey(e => e.UpdatedByUserId);
		});
	}

	private static void ConfigureLogs(ModelBuilder builder)
	{
		builder.Entity<AiRequestLog>(entity =>
		{
			entity.ToTable("AI_REQUEST_LOGS");
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Id).HasColumnName("id");
			entity.Property(e => e.RequestType).HasColumnName("request_type").HasMaxLength(50);
			entity.Property(e => e.Prompt).HasColumnName("prompt");
			entity.Property(e => e.ResponseSummary).HasColumnName("response_summary");
			entity.Property(e => e.RequestedByUserId).HasColumnName("requested_by_user_id");
			entity.Property(e => e.CreatedAt).HasColumnName("created_at");
			entity.HasOne(e => e.RequestedByUser).WithMany().HasForeignKey(e => e.RequestedByUserId);
		});

		builder.Entity<AuditLog>(entity =>
		{
			entity.ToTable("AUDIT_LOGS");
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Id).HasColumnName("id");
			entity.Property(e => e.ActorUserId).HasColumnName("actor_user_id");
			entity.Property(e => e.EntityName).HasColumnName("entity_name").HasMaxLength(100);
			entity.Property(e => e.EntityId).HasColumnName("entity_id");
			entity.Property(e => e.ActionType).HasColumnName("action_type").HasMaxLength(50);
			entity.Property(e => e.OldValues).HasColumnName("old_values");
			entity.Property(e => e.NewValues).HasColumnName("new_values");
			entity.Property(e => e.CreatedAt).HasColumnName("created_at");
			entity.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
			entity.HasOne(e => e.ActorUser).WithMany().HasForeignKey(e => e.ActorUserId);
		});

		builder.Entity<SchedulerJobLog>(entity =>
		{
			entity.ToTable("SCHEDULER_JOB_LOGS");
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Id).HasColumnName("id");
			entity.Property(e => e.JobName).HasColumnName("job_name").HasMaxLength(100);
			entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
			entity.Property(e => e.StartedAt).HasColumnName("started_at");
			entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
			entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
		});
	}

	private static void ConfigureIndexes(ModelBuilder builder)
	{
		builder.Entity<Employee>().HasIndex(e => e.ManagerId).HasDatabaseName("IX_Employees_Manager");
		builder.Entity<ProjectAllocation>().HasIndex(e => new { e.EmployeeId, e.AllocationStatus, e.AllocationStartDate, e.AllocationEndDate }).HasDatabaseName("IX_Allocations_Employee");
		builder.Entity<ProjectAllocation>().HasIndex(e => e.ProjectId).HasDatabaseName("IX_Allocations_Project");
		builder.Entity<Timesheet>().HasIndex(e => e.EmployeeId).HasDatabaseName("IX_Timesheets_Employee_Week");
		builder.Entity<Project>().HasIndex(e => e.ManagerUserId).HasDatabaseName("IX_Projects_Manager");
		builder.Entity<ProjectMilestone>().HasIndex(e => new { e.ProjectId, e.DueDate }).HasDatabaseName("IX_Milestones_Project");
		builder.Entity<TimesheetLineItem>().HasIndex(e => e.TimesheetId).HasDatabaseName("IX_LineItems_Timesheet");
		builder.Entity<AiRequestLog>().HasIndex(e => e.RequestedByUserId).HasDatabaseName("IX_AiLogs_User");
	}
}
