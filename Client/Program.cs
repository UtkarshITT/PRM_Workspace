using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
	.SetBasePath(AppContext.BaseDirectory)
	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
	.Build();

Console.WriteLine("=== PRM Tool — Project & Resource Management ===");
Console.WriteLine($"Server: {configuration["ServerBaseUrl"]}");
Console.WriteLine();
Console.WriteLine("PRM Client (placeholder menu)");
Console.WriteLine("  1. Login (Phase 1)");
Console.WriteLine("  0. Exit");
Console.Write("Select option: ");

var input = Console.ReadLine();
if (input == "0")
{
	Console.WriteLine("Goodbye.");
}
