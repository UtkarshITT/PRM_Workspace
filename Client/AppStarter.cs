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
	private readonly string _serverBaseUrl;

	public AppStarter(
		string serverBaseUrl,
		LoginScreen loginScreen,
		ChangePasswordScreen changePasswordScreen,
		AdminMenuScreen adminMenuScreen,
		ManagerMenuScreen managerMenuScreen,
		EmployeeMenuScreen employeeMenuScreen)
	{
		_serverBaseUrl = serverBaseUrl;
		_loginScreen = loginScreen;
		_changePasswordScreen = changePasswordScreen;
		_adminMenuScreen = adminMenuScreen;
		_managerMenuScreen = managerMenuScreen;
		_employeeMenuScreen = employeeMenuScreen;
	}

	public async Task RunAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("PRM Tool — Project & Resource Management");
		Console.WriteLine($"API: {GetServerUrlHint()}");
		Console.WriteLine("Start the server in another terminal if it is not running yet.");
		Console.WriteLine();

		var loggedIn = await _loginScreen.ShowAsync();
		if (!loggedIn)
		{
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
			case "ADMIN":
				_adminMenuScreen.ShowAsync().GetAwaiter().GetResult();
				break;
			case "MANAGER":
				_managerMenuScreen.Show();
				break;
			case "EMPLOYEE":
				_employeeMenuScreen.Show();
				break;
			default:
				ConsoleHelper.WriteError("Unknown role. Logging out.");
				SessionStore.Clear();
				break;
		}
	}

	private string GetServerUrlHint() => _serverBaseUrl;
}
