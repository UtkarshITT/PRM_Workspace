using PRM.Client.Helpers;
using PRM.Client.HttpClients;

namespace PRM.Client.Screens.Admin;

public class AuditLogsScreen
{
	private const int PageSize = 20;
	private readonly AdminClient _adminClient;

	public AuditLogsScreen(AdminClient adminClient)
	{
		_adminClient = adminClient;
	}

	public async Task ShowAsync()
	{
		var actorUserId = ReadOptionalLong("Actor User ID filter (Enter for all): ");
		var actionType = ReadOptionalText("Action filter (Enter for all): ");
		var entityName = ReadOptionalText("Entity filter (Enter for all): ");
		var entityId = ReadOptionalLong("Entity ID filter (Enter for all): ");
		var from = ReadOptionalDate("From date yyyy-MM-dd (Enter for all): ");
		var to = ReadOptionalDate("To date yyyy-MM-dd (Enter for all): ");
		var page = 1;

		while (true)
		{
			Console.Clear();
			ConsoleHelper.WriteHeader("Audit Logs");

			var response = await _adminClient.GetAuditLogsAsync(
				page,
				PageSize,
				actorUserId,
				actionType,
				entityName,
				entityId,
				from,
				to);

			if (!response.Success || response.Data == null)
			{
				WriteApiError(response);
				return;
			}

			var data = response.Data;
			Console.WriteLine($"{"ID",-6}{"When",-17}{"Actor",-18}{"Action",-20}{"Entity",-18}Notes");
			Console.WriteLine(new string('-', 110));

			foreach (var item in data.Items)
			{
				var when = item.CreatedAt.ToString("dd-MM-yyyy HH:mm");
				var entity = $"{item.EntityName}#{item.EntityId}";
				var notes = item.Notes ?? item.NewValues ?? "-";
				Console.WriteLine(
					$"{item.Id,-6}{when,-17}{Trim(item.ActorUsername, 16),-18}{Trim(item.ActionType, 18),-20}{Trim(entity, 16),-18}{Trim(notes, 36)}");
			}

			Console.WriteLine();
			Console.WriteLine($"Page {data.Page} of {Math.Max(data.TotalPages, 1)} | Total rows: {data.TotalCount}");
			Console.Write("N = next, P = previous, Enter = back: ");
			var choice = Console.ReadLine()?.Trim();

			if (string.IsNullOrWhiteSpace(choice))
			{
				return;
			}

			if (choice.Equals("N", StringComparison.OrdinalIgnoreCase) && page < data.TotalPages)
			{
				page++;
			}
			else if (choice.Equals("P", StringComparison.OrdinalIgnoreCase) && page > 1)
			{
				page--;
			}
		}
	}

	private static long? ReadOptionalLong(string prompt)
	{
		var input = ConsoleHelper.ReadInput(prompt);
		return long.TryParse(input, out var value) ? value : null;
	}

	private static string? ReadOptionalText(string prompt)
	{
		var input = ConsoleHelper.ReadInput(prompt);
		return string.IsNullOrWhiteSpace(input) ? null : input.Trim();
	}

	private static string? ReadOptionalDate(string prompt)
	{
		var input = ConsoleHelper.ReadInput(prompt);
		return DateOnly.TryParse(input, out var value) ? value.ToString("yyyy-MM-dd") : null;
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
