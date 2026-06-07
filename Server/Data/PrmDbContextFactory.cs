using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PRM.Server.Data;

public class PrmDbContextFactory : IDesignTimeDbContextFactory<PrmDbContext>
{
	public PrmDbContext CreateDbContext(string[] args)
	{
		var configuration = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json", optional: false)
			.Build();

		var optionsBuilder = new DbContextOptionsBuilder<PrmDbContext>();
		optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));

		return new PrmDbContext(optionsBuilder.Options);
	}
}
