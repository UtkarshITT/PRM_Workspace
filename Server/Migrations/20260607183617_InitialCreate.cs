using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRM.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ACTIVITY_TAGS",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tag_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    tag_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    tag_category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ACTIVITY_TAGS", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "SCHEDULER_JOB_LOGS",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    job_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    started_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    completed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    error_message = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SCHEDULER_JOB_LOGS", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "SKILLS",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    skill_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SKILLS", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "USERS",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    force_password_change = table.Column<bool>(type: "bit", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USERS", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AI_REQUEST_LOGS",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    request_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    prompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    response_summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    requested_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_REQUEST_LOGS", x => x.id);
                    table.ForeignKey(
                        name: "FK_AI_REQUEST_LOGS_USERS_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalTable: "USERS",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AUDIT_LOGS",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    actor_user_id = table.Column<long>(type: "bigint", nullable: false),
                    entity_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<long>(type: "bigint", nullable: false),
                    action_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    old_values = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    new_values = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    correlation_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AUDIT_LOGS", x => x.id);
                    table.ForeignKey(
                        name: "FK_AUDIT_LOGS_USERS_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "USERS",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EMPLOYEES",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    manager_id = table.Column<long>(type: "bigint", nullable: true),
                    employee_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    designation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    employment_status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    joined_at = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMPLOYEES", x => x.id);
                    table.ForeignKey(
                        name: "FK_EMPLOYEES_USERS_manager_id",
                        column: x => x.manager_id,
                        principalTable: "USERS",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EMPLOYEES_USERS_user_id",
                        column: x => x.user_id,
                        principalTable: "USERS",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PROJECTS",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    project_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    project_status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    health_status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    total_story_points = table.Column<int>(type: "int", nullable: false),
                    manager_user_id = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROJECTS", x => x.id);
                    table.ForeignKey(
                        name: "FK_PROJECTS_USERS_manager_user_id",
                        column: x => x.manager_user_id,
                        principalTable: "USERS",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SYSTEM_CONFIGURATIONS",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    config_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    config_value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SYSTEM_CONFIGURATIONS", x => x.id);
                    table.ForeignKey(
                        name: "FK_SYSTEM_CONFIGURATIONS_USERS_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "USERS",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "EMPLOYEE_SKILLS",
                columns: table => new
                {
                    employee_id = table.Column<long>(type: "bigint", nullable: false),
                    skill_id = table.Column<long>(type: "bigint", nullable: false),
                    proficiency_level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMPLOYEE_SKILLS", x => new { x.employee_id, x.skill_id });
                    table.ForeignKey(
                        name: "FK_EMPLOYEE_SKILLS_EMPLOYEES_employee_id",
                        column: x => x.employee_id,
                        principalTable: "EMPLOYEES",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EMPLOYEE_SKILLS_SKILLS_skill_id",
                        column: x => x.skill_id,
                        principalTable: "SKILLS",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TIMESHEETS",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    employee_id = table.Column<long>(type: "bigint", nullable: false),
                    week_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    total_hours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TIMESHEETS", x => x.id);
                    table.ForeignKey(
                        name: "FK_TIMESHEETS_EMPLOYEES_employee_id",
                        column: x => x.employee_id,
                        principalTable: "EMPLOYEES",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PROJECT_ALLOCATIONS",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    employee_id = table.Column<long>(type: "bigint", nullable: false),
                    project_id = table.Column<long>(type: "bigint", nullable: false),
                    allocation_percentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    allocation_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    allocation_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    allocation_status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    allocated_by_manager_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROJECT_ALLOCATIONS", x => x.id);
                    table.ForeignKey(
                        name: "FK_PROJECT_ALLOCATIONS_EMPLOYEES_employee_id",
                        column: x => x.employee_id,
                        principalTable: "EMPLOYEES",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PROJECT_ALLOCATIONS_PROJECTS_project_id",
                        column: x => x.project_id,
                        principalTable: "PROJECTS",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PROJECT_ALLOCATIONS_USERS_allocated_by_manager_id",
                        column: x => x.allocated_by_manager_id,
                        principalTable: "USERS",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PROJECT_MILESTONES",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_id = table.Column<long>(type: "bigint", nullable: false),
                    milestone_title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    milestone_status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    story_points = table.Column<int>(type: "int", nullable: false),
                    sort_order = table.Column<short>(type: "smallint", nullable: false),
                    completed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROJECT_MILESTONES", x => x.id);
                    table.ForeignKey(
                        name: "FK_PROJECT_MILESTONES_PROJECTS_project_id",
                        column: x => x.project_id,
                        principalTable: "PROJECTS",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TIMESHEET_LINE_ITEMS",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    timesheet_id = table.Column<long>(type: "bigint", nullable: false),
                    project_id = table.Column<long>(type: "bigint", nullable: false),
                    hours_logged = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    work_notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    work_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TIMESHEET_LINE_ITEMS", x => x.id);
                    table.ForeignKey(
                        name: "FK_TIMESHEET_LINE_ITEMS_PROJECTS_project_id",
                        column: x => x.project_id,
                        principalTable: "PROJECTS",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TIMESHEET_LINE_ITEMS_TIMESHEETS_timesheet_id",
                        column: x => x.timesheet_id,
                        principalTable: "TIMESHEETS",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TIMESHEET_LINE_ITEM_ACTIVITY_TAGS",
                columns: table => new
                {
                    timesheet_line_item_id = table.Column<long>(type: "bigint", nullable: false),
                    activity_tag_id = table.Column<long>(type: "bigint", nullable: false),
                    custom_tag_text = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TIMESHEET_LINE_ITEM_ACTIVITY_TAGS", x => new { x.timesheet_line_item_id, x.activity_tag_id });
                    table.ForeignKey(
                        name: "FK_TIMESHEET_LINE_ITEM_ACTIVITY_TAGS_ACTIVITY_TAGS_activity_tag_id",
                        column: x => x.activity_tag_id,
                        principalTable: "ACTIVITY_TAGS",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TIMESHEET_LINE_ITEM_ACTIVITY_TAGS_TIMESHEET_LINE_ITEMS_timesheet_line_item_id",
                        column: x => x.timesheet_line_item_id,
                        principalTable: "TIMESHEET_LINE_ITEMS",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ACTIVITY_TAGS_tag_code",
                table: "ACTIVITY_TAGS",
                column: "tag_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiLogs_User",
                table: "AI_REQUEST_LOGS",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_LOGS_actor_user_id",
                table: "AUDIT_LOGS",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_EMPLOYEE_SKILLS_skill_id",
                table: "EMPLOYEE_SKILLS",
                column: "skill_id");

            migrationBuilder.CreateIndex(
                name: "IX_EMPLOYEES_employee_code",
                table: "EMPLOYEES",
                column: "employee_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Manager",
                table: "EMPLOYEES",
                column: "manager_id");

            migrationBuilder.CreateIndex(
                name: "IX_EMPLOYEES_user_id",
                table: "EMPLOYEES",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_Employee",
                table: "PROJECT_ALLOCATIONS",
                columns: new[] { "employee_id", "allocation_status", "allocation_start_date", "allocation_end_date" });

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_Project",
                table: "PROJECT_ALLOCATIONS",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_PROJECT_ALLOCATIONS_allocated_by_manager_id",
                table: "PROJECT_ALLOCATIONS",
                column: "allocated_by_manager_id");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_Project",
                table: "PROJECT_MILESTONES",
                columns: new[] { "project_id", "due_date" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Manager",
                table: "PROJECTS",
                column: "manager_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_PROJECTS_project_code",
                table: "PROJECTS",
                column: "project_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SKILLS_skill_name",
                table: "SKILLS",
                column: "skill_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SYSTEM_CONFIGURATIONS_config_key",
                table: "SYSTEM_CONFIGURATIONS",
                column: "config_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SYSTEM_CONFIGURATIONS_updated_by_user_id",
                table: "SYSTEM_CONFIGURATIONS",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_TIMESHEET_LINE_ITEM_ACTIVITY_TAGS_activity_tag_id",
                table: "TIMESHEET_LINE_ITEM_ACTIVITY_TAGS",
                column: "activity_tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_LineItems_Timesheet",
                table: "TIMESHEET_LINE_ITEMS",
                column: "timesheet_id");

            migrationBuilder.CreateIndex(
                name: "IX_TIMESHEET_LINE_ITEMS_project_id",
                table: "TIMESHEET_LINE_ITEMS",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_TIMESHEETS_employee_id_week_start_date",
                table: "TIMESHEETS",
                columns: new[] { "employee_id", "week_start_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_Employee_Week",
                table: "TIMESHEETS",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_USERS_email",
                table: "USERS",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USERS_username",
                table: "USERS",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AI_REQUEST_LOGS");

            migrationBuilder.DropTable(
                name: "AUDIT_LOGS");

            migrationBuilder.DropTable(
                name: "EMPLOYEE_SKILLS");

            migrationBuilder.DropTable(
                name: "PROJECT_ALLOCATIONS");

            migrationBuilder.DropTable(
                name: "PROJECT_MILESTONES");

            migrationBuilder.DropTable(
                name: "SCHEDULER_JOB_LOGS");

            migrationBuilder.DropTable(
                name: "SYSTEM_CONFIGURATIONS");

            migrationBuilder.DropTable(
                name: "TIMESHEET_LINE_ITEM_ACTIVITY_TAGS");

            migrationBuilder.DropTable(
                name: "SKILLS");

            migrationBuilder.DropTable(
                name: "ACTIVITY_TAGS");

            migrationBuilder.DropTable(
                name: "TIMESHEET_LINE_ITEMS");

            migrationBuilder.DropTable(
                name: "PROJECTS");

            migrationBuilder.DropTable(
                name: "TIMESHEETS");

            migrationBuilder.DropTable(
                name: "EMPLOYEES");

            migrationBuilder.DropTable(
                name: "USERS");
        }
    }
}
