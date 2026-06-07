using PRM.Client.Helpers;
using PRM.Client.HttpClients;
using PRM.Client.Models.Auth;

namespace PRM.Client.Screens;

public class ChangePasswordScreen
{
	private readonly AuthClient _authClient;

	public ChangePasswordScreen(AuthClient authClient)
	{
		_authClient = authClient;
	}

	public async Task<bool> ShowAsync()
	{
		ConsoleHelper.WriteHeader("Change Password");
		Console.WriteLine("You must change your password before continuing.");
		Console.WriteLine();

		var currentPassword = ConsoleHelper.ReadInput("Current password: ", secret: true);
		var newPassword = ConsoleHelper.ReadInput("New password: ", secret: true);
		var confirmPassword = ConsoleHelper.ReadInput("Confirm new password: ", secret: true);

		if (newPassword != confirmPassword)
		{
			ConsoleHelper.WriteError("New password and confirmation do not match.");
			return false;
		}

		var response = await _authClient.ChangePasswordAsync(new PasswordChangeRequest
		{
			CurrentPassword = currentPassword,
			NewPassword = newPassword
		});

		if (!response.Success || response.Data == null)
		{
			ConsoleHelper.WriteError(response.Error ?? "Password change failed.");
			if (response.Details.Count > 0)
			{
				foreach (var detail in response.Details)
				{
					ConsoleHelper.WriteError($"  - {detail}");
				}
			}

			return false;
		}

		SessionStore.SetSession(response.Data);
		ConsoleHelper.WriteSuccess(response.Message ?? "Password changed successfully.");
		return true;
	}
}
