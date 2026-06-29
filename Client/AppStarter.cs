using PRM.Client.Constants;
using PRM.Client.Helpers;
using PRM.Client.HttpClients;
using PRM.Client.Screens;
using PRM.Client.Screens.Admin;
using PRM.Client.Screens.Employee;
using PRM.Client.Screens.Manager;

namespace PRM.Client;

public class AppStarter
{
	private readonly LoginScreen _loginScreen;
	private readonly ChangePasswordScreen _changePasswordScreen;
	private readonly AdminMenuScreen _adminMenuScreen;
	private readonly ManagerMenuScreen _managerMenuScreen;
	private readonly EmployeeMenuScreen _employeeMenuScreen;

	public AppStarter(
		LoginScreen loginScreen,
		ChangePasswordScreen changePasswordScreen,
		AdminMenuScreen adminMenuScreen,
		ManagerMenuScreen managerMenuScreen,
		EmployeeMenuScreen employeeMenuScreen)
	{
		_loginScreen = loginScreen;
		_changePasswordScreen = changePasswordScreen;
		_adminMenuScreen = adminMenuScreen;
		_managerMenuScreen = managerMenuScreen;
		_employeeMenuScreen = employeeMenuScreen;
	}

	public async Task RunAsync()
	{
		var running = true;

		while (running)
		{
			Console.Clear();
			ConsoleHelper.WriteAppHeader();
			Console.WriteLine("1. Login");
			Console.WriteLine("2. Exit");
			Console.WriteLine();
			Console.Write("Enter option: ");

			switch (Console.ReadLine()?.Trim())
			{
				case "1":
					await LoginAndRouteAsync();
					break;
				case "2":
					running = false;
					break;
				default:
					ConsoleHelper.WriteError("Invalid option.");
					ConsoleHelper.PressEnterToContinue();
					break;
			}
		}
	}

	private async Task LoginAndRouteAsync()
	{
		Console.Clear();
		var loggedIn = await _loginScreen.ShowAsync();
		if (!loggedIn)
		{
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		if (SessionStore.ForcePasswordChange)
		{
			var changed = await PromptPasswordChangeAsync();
			if (!changed)
			{
				SessionStore.Clear();
				return;
			}
		}

		RouteToRoleMenu();
	}

	private async Task<bool> PromptPasswordChangeAsync()
	{
		while (SessionStore.ForcePasswordChange)
		{
			var changed = await _changePasswordScreen.ShowAsync();
			if (changed)
			{
				return true;
			}

			ConsoleHelper.PressEnterToContinue();
		}

		return true;
	}

	private void RouteToRoleMenu()
	{
		switch (SessionStore.Role)
		{
			case Roles.Admin:
				_adminMenuScreen.ShowAsync().GetAwaiter().GetResult();
				break;
			case Roles.Manager:
				_managerMenuScreen.ShowAsync().GetAwaiter().GetResult();
				break;
			case Roles.Employee:
				_employeeMenuScreen.ShowAsync().GetAwaiter().GetResult();
				break;
			default:
				ConsoleHelper.WriteError("Unknown role. Logging out.");
				SessionStore.Clear();
				break;
		}
	}
}
