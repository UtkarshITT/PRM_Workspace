using PRM.Client.Helpers;
using PRM.Client.HttpClients;
using PRM.Client.Models.Auth;

namespace PRM.Client.Screens;

public class LoginScreen
{
	private readonly AuthClient _authClient;

	public LoginScreen(AuthClient authClient)
	{
		_authClient = authClient;
	}

	public async Task<bool> ShowAsync()
	{
		ConsoleHelper.WriteHeader("Login");

		var username = ConsoleHelper.ReadInput("Username: ");
		var password = ConsoleHelper.ReadInput("Password: ", secret: true);

		if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
		{
			ConsoleHelper.WriteError("Username and password are required.");
			return false;
		}

		var response = await _authClient.LoginAsync(new LoginRequest
		{
			Username = username,
			Password = password
		});

		if (!response.Success || response.Data == null)
		{
			ConsoleHelper.WriteError(response.Error ?? "Login failed.");
			return false;
		}

		SessionStore.SetSession(response.Data);
		ConsoleHelper.WriteSuccess(response.Message ?? "Login successful.");
		return true;
	}
}
