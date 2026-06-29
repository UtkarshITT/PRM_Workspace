using System.Globalization;
using PRM.Client.Helpers;
using PRM.Client.HttpClients;

namespace PRM.Client.Screens.Manager;

public class ResourceDashboardScreen
{
	private readonly ManagerClient _managerClient;

	public ResourceDashboardScreen(ManagerClient managerClient)
	{
		_managerClient = managerClient;
	}

	public async Task ShowAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Resource Dashboard");

		var response = await _managerClient.GetMyTeamAsync();
		if (!response.Success || response.Data == null)
		{
			WriteApiError(response);
			return;
		}

		var dashboard = response.Data;

		Console.WriteLine($"ON BENCH  ({dashboard.BenchMembers.Count} employees fully available)");
		Console.WriteLine(new string('-', 70));
		Console.WriteLine($"{"ID",-6}{"Name",-20}{"Department",-14}{"Alloc %",-10}{"Availability",-14}Skills");

		foreach (var member in dashboard.BenchMembers)
		{
			var skills = member.Skills.Count == 0 ? "-" : string.Join(", ", member.Skills);
			Console.WriteLine($"{member.Id,-6}{member.FullName,-20}{member.Department ?? "-",-14}{"0%",-10}{"FULLY",-14}{skills}");
		}

		Console.WriteLine();
		Console.WriteLine("ACTIVE EMPLOYEES");
		Console.WriteLine(new string('-', 70));
		Console.WriteLine($"{"ID",-6}{"Name",-20}{"Alloc %",-10}Availability");

		foreach (var member in dashboard.ActiveMembers)
		{
			var availability = member.AvailabilityPercent == 0 ? "NONE" : $"{member.AvailabilityPercent:0}% free";
			Console.WriteLine($"{member.Id,-6}{member.FullName,-20}{member.CurrentUtilizationPercent,6:0}%   {availability}");
		}

		Console.WriteLine();
		var partialCount = dashboard.ActiveMembers.Count(member => member.AvailabilityPercent > 0);
		Console.WriteLine($"Bench: {dashboard.BenchMembers.Count}   |   Partial: {partialCount}");
		Console.WriteLine();
		Console.Write("Drill into employee by ID (or Enter to skip): ");
		var input = Console.ReadLine()?.Trim();

		if (!string.IsNullOrWhiteSpace(input) && long.TryParse(input, out var employeeId))
		{
			await ShowEmployeeDetailAsync(employeeId);
		}
		else
		{
			ConsoleHelper.PressEnterToContinue();
		}
	}

	private async Task ShowEmployeeDetailAsync(long employeeId)
	{
		var response = await _managerClient.GetTeamMemberAsync(employeeId);
		Console.Clear();

		if (!response.Success || response.Data == null)
		{
			WriteApiError(response);
			return;
		}

		var detail = response.Data;
		ConsoleHelper.WriteHeader(detail.FullName);
		Console.WriteLine($"Department     : {detail.Department ?? "-"}");
		Console.WriteLine($"Current Status : {detail.EmploymentStatus} ({detail.CurrentUtilizationPercent:0}%)");
		Console.WriteLine($"Timesheet Lock : {(detail.IsTimesheetFrozen ? "FROZEN" : "Active")}");
		Console.WriteLine($"Profile Skills : {string.Join(", ", detail.Skills)}");
		Console.WriteLine();
		Console.WriteLine("Current / Upcoming Allocations:");

		if (detail.ActiveAllocations.Count == 0)
		{
			Console.WriteLine("  (none)");
		}
		else
		{
			var today = DateOnly.FromDateTime(DateTime.Today);
			Console.WriteLine($"  {"Alloc ID",-9}{"Project",-18}{"%",-6}{"From",-12}{"To",-12}State");
			foreach (var allocation in detail.ActiveAllocations)
			{
				var from = DateOnly.Parse(allocation.AllocationStartDate, CultureInfo.InvariantCulture);
				var to = DateOnly.Parse(allocation.AllocationEndDate, CultureInfo.InvariantCulture);
				var state = from > today ? "Upcoming" : "Current";
				Console.WriteLine(
					$"  {allocation.AllocationId,-9}{allocation.ProjectName,-18}{allocation.AllocationPercentage,4:0}%  {DateFormatHelper.FormatDisplay(from),-12}{DateFormatHelper.FormatDisplay(to),-12}{state}");
			}
		}

		Console.WriteLine();
		Console.WriteLine("Recent Activity Tags (last 4 weeks):");
		Console.WriteLine(detail.RecentActivityTags.Count == 0
			? "  (none)"
			: $"  {string.Join(", ", detail.RecentActivityTags)}");

		if (detail.IsTimesheetFrozen)
		{
			Console.WriteLine();
			var confirm = ConsoleHelper.ReadInput("Restore timesheet access now? (Y/N): ");
			if (confirm.Equals("Y", StringComparison.OrdinalIgnoreCase))
			{
				var restore = await _managerClient.RestoreTimesheetAccessAsync(employeeId);
				if (restore.Success)
				{
					ConsoleHelper.WriteSuccess("Timesheet access restored.");
				}
				else
				{
					WriteApiError(restore);
					return;
				}
			}
		}

		ConsoleHelper.PressEnterToContinue();
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
