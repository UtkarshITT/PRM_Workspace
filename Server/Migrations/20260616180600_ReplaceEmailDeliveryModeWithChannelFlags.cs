using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRM.Server.Migrations
{
	/// <inheritdoc />
	public partial class ReplaceEmailDeliveryModeWithChannelFlags : Migration
	{
		private static readonly DateTime SeedTimestamp = new(2026, 6, 16, 18, 6, 0, DateTimeKind.Utc);

		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			var seedTime = SeedTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fffffff");

			migrationBuilder.Sql($"""
				DELETE FROM [SYSTEM_CONFIGURATIONS] WHERE [config_key] = 'email_delivery_mode';

				IF NOT EXISTS (SELECT 1 FROM [SYSTEM_CONFIGURATIONS] WHERE [config_key] = 'email_console_enabled')
				    INSERT INTO [SYSTEM_CONFIGURATIONS] ([config_key], [config_value], [description], [updated_at], [updated_by_user_id])
				    VALUES ('email_console_enabled', 'true', 'Log outbound emails to console/Serilog (runs alongside SMTP when both enabled)', '{seedTime}', NULL);

				IF NOT EXISTS (SELECT 1 FROM [SYSTEM_CONFIGURATIONS] WHERE [config_key] = 'email_smtp_enabled')
				    INSERT INTO [SYSTEM_CONFIGURATIONS] ([config_key], [config_value], [description], [updated_at], [updated_by_user_id])
				    VALUES ('email_smtp_enabled', 'true', 'Send outbound emails via SMTP when server SMTP config is present (runs alongside console when both enabled)', '{seedTime}', NULL);
				""");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			var seedTime = SeedTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fffffff");

			migrationBuilder.Sql($"""
				DELETE FROM [SYSTEM_CONFIGURATIONS]
				WHERE [config_key] IN ('email_console_enabled', 'email_smtp_enabled');

				IF NOT EXISTS (SELECT 1 FROM [SYSTEM_CONFIGURATIONS] WHERE [config_key] = 'email_delivery_mode')
				    INSERT INTO [SYSTEM_CONFIGURATIONS] ([config_key], [config_value], [description], [updated_at], [updated_by_user_id])
				    VALUES ('email_delivery_mode', 'console', 'Email delivery: console (dev) or smtp (prod)', '{seedTime}', NULL);
				""");
		}
	}
}
