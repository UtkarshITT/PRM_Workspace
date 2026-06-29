using PRM.Client.Helpers;
using PRM.Client.HttpClients;
using PRM.Client.Models.Manager;

namespace PRM.Client.Screens.Manager;

public class FrozenTimesheetAccessScreen
{
	private readonly ManagerClient _managerClient;

	public FrozenTimesheetAccessScreen(ManagerClient managerClient)
	{
		_managerClient = managerClient;
	}

	public async Task ShowAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Frozen Timesheet Access");

		var response = await _managerClient.GetMyTeamAsync();
		if (!response.Success || response.Data == null)
		{
			WriteApiError(response);
			return;
		}

		var frozenMembers = GetFrozenMembers(response.Data);
		if (frozenMembers.Count == 0)
		{
			Console.WriteLine("No frozen timesheet access found for your team.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		Console.WriteLine($"{"ID",-6}{"Name",-22}{"Status",-12}{"Util %",-10}Frozen At");
		Console.WriteLine(new string('-', 72));
		foreach (var member in frozenMembers)
		{
			Console.WriteLine(
				$"{member.Id,-6}{member.FullName,-22}{member.Status,-12}{member.UtilizationPercent,6:0}%   {FormatFrozenAt(member.TimesheetFrozenAt)}");
		}

		Console.WriteLine();
		if (!long.TryParse(ConsoleHelper.ReadInput("Employee ID to restore (or 0 to cancel): "), out var employeeId)
			|| employeeId == 0)
		{
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		if (frozenMembers.All(member => member.Id != employeeId))
		{
			ConsoleHelper.WriteError("Selected employee does not have frozen timesheet access in your team.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var confirm = ConsoleHelper.ReadInput("Restore timesheet access now? (Y/N): ");
		if (!confirm.Equals("Y", StringComparison.OrdinalIgnoreCase))
		{
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var restoreResponse = await _managerClient.RestoreTimesheetAccessAsync(employeeId);
		if (restoreResponse.Success)
		{
			ConsoleHelper.WriteSuccess("Timesheet access restored.");
		}
		else
		{
			WriteApiError(restoreResponse);
			return;
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private static List<FrozenTeamMember> GetFrozenMembers(TeamDashboardResponse dashboard)
	{
		var frozenMembers = new List<FrozenTeamMember>();

		frozenMembers.AddRange(dashboard.BenchMembers
			.Where(member => member.IsTimesheetFrozen)
			.Select(member => new FrozenTeamMember(
				member.Id,
				member.FullName,
				"BENCH",
				0,
				member.TimesheetFrozenAt)));

		frozenMembers.AddRange(dashboard.ActiveMembers
			.Where(member => member.IsTimesheetFrozen)
			.Select(member => new FrozenTeamMember(
				member.Id,
				member.FullName,
				"ALLOCATED",
				member.CurrentUtilizationPercent,
				member.TimesheetFrozenAt)));

		return frozenMembers
			.OrderBy(member => member.FullName)
			.ToList();
	}

	private static string FormatFrozenAt(DateTime? frozenAt)
	{
		return frozenAt.HasValue ? frozenAt.Value.ToString("dd-MM-yyyy HH:mm") : "-";
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

	private sealed record FrozenTeamMember(
		long Id,
		string FullName,
		string Status,
		decimal UtilizationPercent,
		DateTime? TimesheetFrozenAt);
}
