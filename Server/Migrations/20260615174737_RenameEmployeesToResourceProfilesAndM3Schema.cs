using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRM.Server.Migrations
{
	/// <inheritdoc />
	public partial class RenameEmployeesToResourceProfilesAndM3Schema : Migration
	{
		private static readonly DateTime SeedTimestamp = new(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);

		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "FK_PROJECT_ALLOCATIONS_EMPLOYEES_employee_id",
				table: "PROJECT_ALLOCATIONS");

			migrationBuilder.DropForeignKey(
				name: "FK_TIMESHEETS_EMPLOYEES_employee_id",
				table: "TIMESHEETS");

			migrationBuilder.DropForeignKey(
				name: "FK_EMPLOYEE_SKILLS_EMPLOYEES_employee_id",
				table: "EMPLOYEE_SKILLS");

			migrationBuilder.RenameTable(
				name: "EMPLOYEES",
				newName: "RESOURCE_PROFILES");

			migrationBuilder.RenameTable(
				name: "EMPLOYEE_SKILLS",
				newName: "RESOURCE_PROFILE_SKILLS");

			migrationBuilder.RenameColumn(
				name: "employee_code",
				table: "RESOURCE_PROFILES",
				newName: "resource_profile_code");

			migrationBuilder.RenameIndex(
				name: "IX_EMPLOYEES_employee_code",
				table: "RESOURCE_PROFILES",
				newName: "IX_RESOURCE_PROFILES_resource_profile_code");

			migrationBuilder.RenameIndex(
				name: "IX_EMPLOYEES_user_id",
				table: "RESOURCE_PROFILES",
				newName: "IX_RESOURCE_PROFILES_user_id");

			migrationBuilder.RenameIndex(
				name: "IX_Employees_Manager",
				table: "RESOURCE_PROFILES",
				newName: "IX_ResourceProfiles_Manager");

			migrationBuilder.RenameColumn(
				name: "employee_id",
				table: "RESOURCE_PROFILE_SKILLS",
				newName: "resource_profile_id");

			migrationBuilder.RenameColumn(
				name: "employee_id",
				table: "TIMESHEETS",
				newName: "resource_profile_id");

			migrationBuilder.RenameIndex(
				name: "IX_Timesheets_Employee_Week",
				table: "TIMESHEETS",
				newName: "IX_Timesheets_ResourceProfile_Week");

			migrationBuilder.RenameIndex(
				name: "IX_TIMESHEETS_employee_id_week_start_date",
				table: "TIMESHEETS",
				newName: "IX_TIMESHEETS_resource_profile_id_week_start_date");

			migrationBuilder.RenameColumn(
				name: "employee_id",
				table: "PROJECT_ALLOCATIONS",
				newName: "resource_profile_id");

			migrationBuilder.RenameIndex(
				name: "IX_Allocations_Employee",
				table: "PROJECT_ALLOCATIONS",
				newName: "IX_Allocations_ResourceProfile");

			migrationBuilder.AddColumn<bool>(
				name: "is_timesheet_frozen",
				table: "RESOURCE_PROFILES",
				type: "bit",
				nullable: false,
				defaultValue: false);

			migrationBuilder.AddColumn<DateTime>(
				name: "timesheet_frozen_at",
				table: "RESOURCE_PROFILES",
				type: "datetime2",
				nullable: true);

			migrationBuilder.AddColumn<string>(
				name: "last_risk_summary",
				table: "PROJECTS",
				type: "nvarchar(max)",
				nullable: true);

			migrationBuilder.CreateTable(
				name: "NOTIFICATION_LOGS",
				columns: table => new
				{
					id = table.Column<long>(type: "bigint", nullable: false)
						.Annotation("SqlServer:Identity", "1, 1"),
					notification_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
					recipient_user_id = table.Column<long>(type: "bigint", nullable: false),
					recipient_email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
					subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
					body = table.Column<string>(type: "nvarchar(max)", nullable: false),
					status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
					delivery_channel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
					related_entity_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
					related_entity_id = table.Column<long>(type: "bigint", nullable: true),
					week_start_date = table.Column<DateOnly>(type: "date", nullable: true),
					error_message = table.Column<string>(type: "nvarchar(max)", nullable: true),
					sent_at = table.Column<DateTime>(type: "datetime2", nullable: true),
					created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_NOTIFICATION_LOGS", x => x.id);
					table.ForeignKey(
						name: "FK_NOTIFICATION_LOGS_USERS_recipient_user_id",
						column: x => x.recipient_user_id,
						principalTable: "USERS",
						principalColumn: "id",
						onDelete: ReferentialAction.Restrict);
				});

			migrationBuilder.CreateTable(
				name: "TIMESHEET_COMPLIANCE_TRACKING",
				columns: table => new
				{
					resource_profile_id = table.Column<long>(type: "bigint", nullable: false),
					week_start_date = table.Column<DateOnly>(type: "date", nullable: false),
					reminder_count = table.Column<short>(type: "smallint", nullable: false),
					last_reminder_at = table.Column<DateTime>(type: "datetime2", nullable: true),
					is_frozen_for_week = table.Column<bool>(type: "bit", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_TIMESHEET_COMPLIANCE_TRACKING", x => new { x.resource_profile_id, x.week_start_date });
					table.ForeignKey(
						name: "FK_TIMESHEET_COMPLIANCE_TRACKING_RESOURCE_PROFILES_resource_profile_id",
						column: x => x.resource_profile_id,
						principalTable: "RESOURCE_PROFILES",
						principalColumn: "id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_NOTIFICATION_LOGS_recipient_user_id",
				table: "NOTIFICATION_LOGS",
				column: "recipient_user_id");

			migrationBuilder.AddForeignKey(
				name: "FK_RESOURCE_PROFILE_SKILLS_RESOURCE_PROFILES_resource_profile_id",
				table: "RESOURCE_PROFILE_SKILLS",
				column: "resource_profile_id",
				principalTable: "RESOURCE_PROFILES",
				principalColumn: "id",
				onDelete: ReferentialAction.Cascade);

			migrationBuilder.AddForeignKey(
				name: "FK_PROJECT_ALLOCATIONS_RESOURCE_PROFILES_resource_profile_id",
				table: "PROJECT_ALLOCATIONS",
				column: "resource_profile_id",
				principalTable: "RESOURCE_PROFILES",
				principalColumn: "id",
				onDelete: ReferentialAction.Restrict);

			migrationBuilder.AddForeignKey(
				name: "FK_TIMESHEETS_RESOURCE_PROFILES_resource_profile_id",
				table: "TIMESHEETS",
				column: "resource_profile_id",
				principalTable: "RESOURCE_PROFILES",
				principalColumn: "id",
				onDelete: ReferentialAction.Cascade);

			var seedTime = SeedTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fffffff");
			migrationBuilder.Sql($"""
				IF NOT EXISTS (SELECT 1 FROM [SYSTEM_CONFIGURATIONS] WHERE [config_key] = 'email_console_enabled')
				BEGIN
				    INSERT INTO [SYSTEM_CONFIGURATIONS] ([config_key], [config_value], [description], [updated_at], [updated_by_user_id]) VALUES
				    ('email_console_enabled', 'true',  'Log outbound emails to console/Serilog (runs alongside SMTP)', '{seedTime}', NULL),
				    ('email_smtp_enabled',    'true',  'Send outbound emails via SMTP when configured (runs alongside console)', '{seedTime}', NULL);
				END
				""");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("""
				DELETE FROM [SYSTEM_CONFIGURATIONS]
				WHERE [config_key] IN ('email_console_enabled', 'email_smtp_enabled');
				""");

			migrationBuilder.DropForeignKey(
				name: "FK_TIMESHEETS_RESOURCE_PROFILES_resource_profile_id",
				table: "TIMESHEETS");

			migrationBuilder.DropForeignKey(
				name: "FK_PROJECT_ALLOCATIONS_RESOURCE_PROFILES_resource_profile_id",
				table: "PROJECT_ALLOCATIONS");

			migrationBuilder.DropForeignKey(
				name: "FK_RESOURCE_PROFILE_SKILLS_RESOURCE_PROFILES_resource_profile_id",
				table: "RESOURCE_PROFILE_SKILLS");

			migrationBuilder.DropTable(name: "NOTIFICATION_LOGS");
			migrationBuilder.DropTable(name: "TIMESHEET_COMPLIANCE_TRACKING");

			migrationBuilder.DropColumn(name: "last_risk_summary", table: "PROJECTS");
			migrationBuilder.DropColumn(name: "timesheet_frozen_at", table: "RESOURCE_PROFILES");
			migrationBuilder.DropColumn(name: "is_timesheet_frozen", table: "RESOURCE_PROFILES");

			migrationBuilder.RenameIndex(
				name: "IX_Allocations_ResourceProfile",
				table: "PROJECT_ALLOCATIONS",
				newName: "IX_Allocations_Employee");

			migrationBuilder.RenameColumn(
				name: "resource_profile_id",
				table: "PROJECT_ALLOCATIONS",
				newName: "employee_id");

			migrationBuilder.RenameIndex(
				name: "IX_TIMESHEETS_resource_profile_id_week_start_date",
				table: "TIMESHEETS",
				newName: "IX_TIMESHEETS_employee_id_week_start_date");

			migrationBuilder.RenameIndex(
				name: "IX_Timesheets_ResourceProfile_Week",
				table: "TIMESHEETS",
				newName: "IX_Timesheets_Employee_Week");

			migrationBuilder.RenameColumn(
				name: "resource_profile_id",
				table: "TIMESHEETS",
				newName: "employee_id");

			migrationBuilder.RenameColumn(
				name: "resource_profile_id",
				table: "RESOURCE_PROFILE_SKILLS",
				newName: "employee_id");

			migrationBuilder.RenameIndex(
				name: "IX_ResourceProfiles_Manager",
				table: "RESOURCE_PROFILES",
				newName: "IX_Employees_Manager");

			migrationBuilder.RenameIndex(
				name: "IX_RESOURCE_PROFILES_user_id",
				table: "RESOURCE_PROFILES",
				newName: "IX_EMPLOYEES_user_id");

			migrationBuilder.RenameIndex(
				name: "IX_RESOURCE_PROFILES_resource_profile_code",
				table: "RESOURCE_PROFILES",
				newName: "IX_EMPLOYEES_employee_code");

			migrationBuilder.RenameColumn(
				name: "resource_profile_code",
				table: "RESOURCE_PROFILES",
				newName: "employee_code");

			migrationBuilder.RenameTable(
				name: "RESOURCE_PROFILE_SKILLS",
				newName: "EMPLOYEE_SKILLS");

			migrationBuilder.RenameTable(
				name: "RESOURCE_PROFILES",
				newName: "EMPLOYEES");

			migrationBuilder.AddForeignKey(
				name: "FK_EMPLOYEE_SKILLS_EMPLOYEES_employee_id",
				table: "EMPLOYEE_SKILLS",
				column: "employee_id",
				principalTable: "EMPLOYEES",
				principalColumn: "id",
				onDelete: ReferentialAction.Cascade);

			migrationBuilder.AddForeignKey(
				name: "FK_PROJECT_ALLOCATIONS_EMPLOYEES_employee_id",
				table: "PROJECT_ALLOCATIONS",
				column: "employee_id",
				principalTable: "EMPLOYEES",
				principalColumn: "id",
				onDelete: ReferentialAction.Restrict);

			migrationBuilder.AddForeignKey(
				name: "FK_TIMESHEETS_EMPLOYEES_employee_id",
				table: "TIMESHEETS",
				column: "employee_id",
				principalTable: "EMPLOYEES",
				principalColumn: "id",
				onDelete: ReferentialAction.Cascade);
		}
	}
}
