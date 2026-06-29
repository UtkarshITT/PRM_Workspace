using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRM.Server.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSmtpCredentialsFromSystemConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM [SYSTEM_CONFIGURATIONS]
                WHERE [config_key] IN ('smtp_host', 'smtp_port', 'smtp_username', 'smtp_password', 'smtp_from_address');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [SYSTEM_CONFIGURATIONS] WHERE [config_key] = 'smtp_host')
                BEGIN
                    INSERT INTO [SYSTEM_CONFIGURATIONS] ([config_key], [config_value], [description], [updated_at], [updated_by_user_id]) VALUES
                    ('smtp_host', '', 'SMTP server hostname', SYSUTCDATETIME(), NULL),
                    ('smtp_port', '587', 'SMTP server port', SYSUTCDATETIME(), NULL),
                    ('smtp_username', '', 'SMTP username', SYSUTCDATETIME(), NULL),
                    ('smtp_password', '', 'SMTP password', SYSUTCDATETIME(), NULL),
                    ('smtp_from_address', '', 'From address for outbound emails', SYSUTCDATETIME(), NULL);
                END
                """);
        }
    }
}
