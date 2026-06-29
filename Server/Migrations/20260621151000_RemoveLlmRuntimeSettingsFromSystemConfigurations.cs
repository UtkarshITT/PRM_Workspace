using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRM.Server.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLlmRuntimeSettingsFromSystemConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM [SYSTEM_CONFIGURATIONS]
                WHERE [config_key] IN ('llm_base_url', 'llm_model');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
