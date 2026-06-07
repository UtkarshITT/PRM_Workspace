using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRM.Server.Migrations
{
	/// <inheritdoc />
	public partial class SeedReferenceData : Migration
	{
		private static readonly DateTime SeedTimestamp = new(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc);

		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			var seedTime = SeedTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fffffff");

			migrationBuilder.Sql($"""
				IF NOT EXISTS (SELECT 1 FROM [ACTIVITY_TAGS])
				BEGIN
				    SET IDENTITY_INSERT [ACTIVITY_TAGS] ON;
				    INSERT INTO [ACTIVITY_TAGS] ([id], [tag_code], [tag_name], [tag_category], [is_active], [created_at]) VALUES
				    (1,  'BACKEND_API',   'Backend API Development',          'Backend',  1, '{seedTime}'),
				    (2,  'MICROSERVICES', 'Microservices / Architecture',     'Backend',  1, '{seedTime}'),
				    (3,  'DATABASE',      'Database Design & Queries',        'Backend',  1, '{seedTime}'),
				    (4,  'WEBSOCKET',     'WebSocket / Real-time Features',   'Backend',  1, '{seedTime}'),
				    (5,  'FRONTEND',      'Frontend Development',             'Frontend', 1, '{seedTime}'),
				    (6,  'CODE_REVIEW',   'Code Review / Mentoring',          'General',  1, '{seedTime}'),
				    (7,  'BUG_FIX',       'Bug Fixing',                       'General',  1, '{seedTime}'),
				    (8,  'DEVOPS',        'DevOps / Deployment',              'DevOps',   1, '{seedTime}'),
				    (9,  'TESTING',       'Testing & QA',                     'QA',       1, '{seedTime}'),
				    (10, 'DOCUMENTATION', 'Documentation',                    'General',  1, '{seedTime}'),
				    (11, 'OTHER',         'Other',                              'Other',    1, '{seedTime}');
				    SET IDENTITY_INSERT [ACTIVITY_TAGS] OFF;
				END
				""");

			migrationBuilder.Sql($"""
				IF NOT EXISTS (SELECT 1 FROM [SYSTEM_CONFIGURATIONS])
				BEGIN
				    SET IDENTITY_INSERT [SYSTEM_CONFIGURATIONS] ON;
				    INSERT INTO [SYSTEM_CONFIGURATIONS] ([id], [config_key], [config_value], [description], [updated_at], [updated_by_user_id]) VALUES
				    (1, 'llm_provider',             'Gemini', 'Active LLM provider: Gemini or Groq', '{seedTime}', NULL),
				    (2, 'llm_api_key',              '',       'Encrypted API key for active LLM provider', '{seedTime}', NULL),
				    (3, 'scheduler_interval_hours', '4',      'Background scheduler interval in hours', '{seedTime}', NULL),
				    (4, 'max_weekly_hours',         '40',     'Maximum billable hours per employee per week', '{seedTime}', NULL);
				    SET IDENTITY_INSERT [SYSTEM_CONFIGURATIONS] OFF;
				END
				""");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("""
				DELETE FROM [SYSTEM_CONFIGURATIONS]
				WHERE [config_key] IN ('llm_provider', 'llm_api_key', 'scheduler_interval_hours', 'max_weekly_hours');
				""");

			migrationBuilder.Sql("""
				DELETE FROM [ACTIVITY_TAGS]
				WHERE [tag_code] IN (
				    'BACKEND_API', 'MICROSERVICES', 'DATABASE', 'WEBSOCKET', 'FRONTEND',
				    'CODE_REVIEW', 'BUG_FIX', 'DEVOPS', 'TESTING', 'DOCUMENTATION', 'OTHER');
				""");
		}
	}
}
