using PRM.Client.Helpers;

namespace PRM.Client.Screens.Manager;

public class ManagerMenuScreen
{
	private readonly ResourceDashboardScreen _resourceDashboardScreen;
	private readonly AllocateResourceScreen _allocateResourceScreen;

	public ManagerMenuScreen(
		ResourceDashboardScreen resourceDashboardScreen,
		AllocateResourceScreen allocateResourceScreen)
	{
		_resourceDashboardScreen = resourceDashboardScreen;
		_allocateResourceScreen = allocateResourceScreen;
	}

	public async Task ShowAsync()
	{
		var running = true;

		while (running)
		{
			Console.Clear();
			ConsoleHelper.WriteHeader($"Manager Menu — Welcome, {SessionStore.FullName}");
			Console.WriteLine("  1. Resource Dashboard");
			Console.WriteLine("  2. Allocate Resource");
			Console.WriteLine("  3. My Projects (Phase 6)");
			Console.WriteLine("  4. Team Timesheets (Phase 6)");
			Console.WriteLine("  5. AI Assistant (Phase 8)");
			Console.WriteLine("  0. Logout");
			Console.WriteLine();
			Console.Write("Select option: ");

			switch (Console.ReadLine()?.Trim())
			{
				case "1":
					await _resourceDashboardScreen.ShowAsync();
					break;
				case "2":
					await _allocateResourceScreen.ShowAsync();
					break;
				case "0":
					SessionStore.Clear();
					running = false;
					break;
				default:
					ConsoleHelper.WriteError("Feature coming in a later phase.");
					ConsoleHelper.PressEnterToContinue();
					break;
			}
		}
	}
}
