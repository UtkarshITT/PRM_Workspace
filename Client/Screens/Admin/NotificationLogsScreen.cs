using PRM.Client.Helpers;
using PRM.Client.HttpClients;

namespace PRM.Client.Screens.Admin;

public class NotificationLogsScreen
{
	private readonly AdminClient _adminClient;

	public NotificationLogsScreen(AdminClient adminClient)
	{
		_adminClient = adminClient;
	}

	public async Task ShowAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Notification Logs");

		var response = await _adminClient.GetNotificationLogsAsync();
		if (!response.Success || response.Data == null)
		{
			WriteApiError(response);
			return;
		}

		Console.WriteLine($"{"ID",-6}{"Type",-22}{"Status",-10}{"Channel",-10}{"Recipient",-24}Subject");
		Console.WriteLine(new string('-', 110));

		foreach (var log in response.Data)
		{
			Console.WriteLine(
				$"{log.Id,-6}{log.NotificationType,-22}{log.Status,-10}{log.DeliveryChannel,-10}{Trim(log.RecipientName, 22),-24}{Trim(log.Subject, 40)}");
		}

		Console.WriteLine();
		Console.WriteLine($"Showing latest {response.Data.Count} notification log rows.");
		ConsoleHelper.PressEnterToContinue();
	}

	private static string Trim(string value, int maxLength)
	{
		if (value.Length <= maxLength)
		{
			return value;
		}

		return value[..(maxLength - 3)] + "...";
	}

	private static void WriteApiError<T>(Models.ApiResponse<T> response)
	{
		ConsoleHelper.WriteError(response.Error ?? "Request failed.");

		foreach (var detail in response.Details)
		{
			ConsoleHelper.WriteError($"  - {detail}");
		}

		ConsoleHelper.PressEnterToContinue();
	}
}
