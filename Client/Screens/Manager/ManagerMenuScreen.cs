using PRM.Client.Helpers;

namespace PRM.Client.Screens.Manager;

public class ManagerMenuScreen
{
	private readonly ResourceDashboardScreen _resourceDashboardScreen;
	private readonly AllocateResourceScreen _allocateResourceScreen;
	private readonly MyProjectsScreen _myProjectsScreen;
	private readonly TeamTimesheetsScreen _teamTimesheetsScreen;
	private readonly AiAssistantScreen _aiAssistantScreen;
	private readonly FrozenTimesheetAccessScreen _frozenTimesheetAccessScreen;

	public ManagerMenuScreen(
		ResourceDashboardScreen resourceDashboardScreen,
		AllocateResourceScreen allocateResourceScreen,
		MyProjectsScreen myProjectsScreen,
		TeamTimesheetsScreen teamTimesheetsScreen,
		AiAssistantScreen aiAssistantScreen,
		FrozenTimesheetAccessScreen frozenTimesheetAccessScreen)
	{
		_resourceDashboardScreen = resourceDashboardScreen;
		_allocateResourceScreen = allocateResourceScreen;
		_myProjectsScreen = myProjectsScreen;
		_teamTimesheetsScreen = teamTimesheetsScreen;
		_aiAssistantScreen = aiAssistantScreen;
		_frozenTimesheetAccessScreen = frozenTimesheetAccessScreen;
	}

	public async Task ShowAsync()
	{
		var running = true;

		while (running)
		{
			Console.Clear();
			ConsoleHelper.WriteWelcomeHeader("Manager Menu", SessionStore.FullName);
			Console.WriteLine("  1. Resource Dashboard");
			Console.WriteLine("  2. Allocate Resource");
			Console.WriteLine("  3. My Projects");
			Console.WriteLine("  4. Timesheets");
			Console.WriteLine("  5. AI Assistant");
			Console.WriteLine("  6. Frozen Timesheet Access");
			Console.WriteLine("  7. Logout");
			Console.WriteLine();
			Console.Write("Enter option: ");

			switch (Console.ReadLine()?.Trim())
			{
				case "1":
					await _resourceDashboardScreen.ShowAsync();
					break;
				case "2":
					await _allocateResourceScreen.ShowAsync();
					break;
				case "3":
					await _myProjectsScreen.ShowAsync();
					break;
				case "4":
					await _teamTimesheetsScreen.ShowAsync();
					break;
				case "5":
					await _aiAssistantScreen.ShowAsync();
					break;
				case "6":
					await _frozenTimesheetAccessScreen.ShowAsync();
					break;
				case "7":
					SessionStore.Clear();
					running = false;
					break;
				default:
					ConsoleHelper.WriteError("Invalid option.");
					ConsoleHelper.PressEnterToContinue();
					break;
			}
		}
	}
}
