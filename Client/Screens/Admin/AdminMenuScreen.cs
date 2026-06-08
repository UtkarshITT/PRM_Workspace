using PRM.Client.Helpers;

namespace PRM.Client.Screens.Admin;

public class AdminMenuScreen
{
	private readonly ManageUsersScreen _manageUsersScreen;
	private readonly ManageEmployeesScreen _manageEmployeesScreen;
	private readonly ManageProjectsScreen _manageProjectsScreen;
	private readonly ViewAllocationsScreen _viewAllocationsScreen;

	public AdminMenuScreen(
		ManageUsersScreen manageUsersScreen,
		ManageEmployeesScreen manageEmployeesScreen,
		ManageProjectsScreen manageProjectsScreen,
		ViewAllocationsScreen viewAllocationsScreen)
	{
		_manageUsersScreen = manageUsersScreen;
		_manageEmployeesScreen = manageEmployeesScreen;
		_manageProjectsScreen = manageProjectsScreen;
		_viewAllocationsScreen = viewAllocationsScreen;
	}

	public async Task ShowAsync()
	{
		var running = true;

		while (running)
		{
			Console.Clear();
			ConsoleHelper.WriteHeader($"Admin Panel — Welcome, {SessionStore.FullName}");
			Console.WriteLine("  1. Manage Employees");
			Console.WriteLine("  2. Manage Projects");
			Console.WriteLine("  3. View All Allocations");
			Console.WriteLine("  4. Manage Users");
			Console.WriteLine("  5. System Configuration (Phase 9)");
			Console.WriteLine("  0. Logout");
			Console.WriteLine();
			Console.Write("Select option: ");

			switch (Console.ReadLine()?.Trim())
			{
				case "1":
					await _manageEmployeesScreen.ShowAsync();
					break;
				case "2":
					await _manageProjectsScreen.ShowAsync();
					break;
				case "3":
					await _viewAllocationsScreen.ShowAsync();
					break;
				case "4":
					await _manageUsersScreen.ShowAsync();
					break;
				case "0":
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
