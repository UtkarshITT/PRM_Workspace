using PRM.Client.Helpers;
using PRM.Client.HttpClients;
using PRM.Client.Models.Manager;

namespace PRM.Client.Screens.Manager;

public class MyProjectsScreen
{
	private readonly ManagerClient _managerClient;
	private readonly AiClient _aiClient;

	public MyProjectsScreen(ManagerClient managerClient, AiClient aiClient)
	{
		_managerClient = managerClient;
		_aiClient = aiClient;
	}

	public async Task ShowAsync()
	{
		var running = true;

		while (running)
		{
			Console.Clear();
			ConsoleHelper.WriteHeader("My Projects");

			var response = await _managerClient.GetMyProjectsAsync();
			if (!response.Success || response.Data == null)
			{
				ConsoleHelper.WriteError(response.Error ?? "Failed to load projects.");
				ConsoleHelper.PressEnterToContinue();
				return;
			}

			var projects = response.Data;
			if (projects.Count == 0)
			{
				Console.WriteLine("No projects assigned to you.");
			}
			else
			{
				Console.WriteLine($"{"#",3}  {"Project",-16} {"End Date",-11} Health");
				Console.WriteLine(new string('─', 46));
				for (var index = 0; index < projects.Count; index++)
				{
					var project = projects[index];
					var endDate = DateFormatHelper.ParseApiDate(project.EndDate);
					var healthLabel = HealthStatusHelper.ToDisplayLabel(project.HealthStatus);
					var icon = HealthStatusHelper.ToDisplayIcon(project.HealthStatus);
					Console.WriteLine(
						$"{index + 1,2}.  {project.ProjectName,-16} {DateFormatHelper.FormatDisplay(endDate),-11} {icon} {healthLabel}");
				}
			}

			Console.WriteLine();
			Console.Write("Select project number to view details (0 = Back): ");
			if (!int.TryParse(Console.ReadLine()?.Trim(), out var selection))
			{
				ConsoleHelper.WriteError("Invalid selection.");
				ConsoleHelper.PressEnterToContinue();
				continue;
			}

			if (selection == 0)
			{
				running = false;
				continue;
			}

			if (selection < 1 || selection > projects.Count)
			{
				ConsoleHelper.WriteError("Invalid project number.");
				ConsoleHelper.PressEnterToContinue();
				continue;
			}

			await ShowProjectDetailAsync(projects[selection - 1].Id);
		}
	}

	private async Task ShowProjectDetailAsync(long projectId)
	{
		var response = await _managerClient.GetProjectDetailAsync(projectId);
		if (!response.Success || response.Data == null)
		{
			ConsoleHelper.WriteError(response.Error ?? "Failed to load project detail.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var detail = response.Data;
		Console.Clear();
		ConsoleHelper.WriteHeader(detail.ProjectName);
		var icon = HealthStatusHelper.ToDisplayIcon(detail.HealthStatus);
		var label = HealthStatusHelper.ToDisplayLabel(detail.HealthStatus);
		Console.WriteLine($"Health Status : {icon} {label}");
		Console.WriteLine();

		Console.WriteLine("Risk Flags:");
		if (detail.RiskFlags.Count == 0)
		{
			Console.WriteLine("  No risk flags identified.");
		}
		else
		{
			foreach (var flag in detail.RiskFlags)
			{
				var prefix = flag.IsPositive ? "✓" : "✗";
				Console.WriteLine($"  {prefix}  {flag.Message}");
			}
		}

		Console.WriteLine();
		Console.WriteLine("Milestones:");
		Console.WriteLine($"  {"#",3}  {"Title",-18} {"Due Date",-11} Status");
		foreach (var milestone in detail.Milestones.OrderBy(item => item.SortOrder))
		{
			var dueDate = DateFormatHelper.ParseApiDate(milestone.DueDate);
			var overdueSuffix = milestone.IsOverdue ? "  ⚠ OVERDUE" : string.Empty;
			Console.WriteLine(
				$"  {milestone.SortOrder,2}.  {milestone.MilestoneTitle,-18} {DateFormatHelper.FormatDisplay(dueDate),-11} {milestone.MilestoneStatus}{overdueSuffix}");
		}

		Console.WriteLine();
		Console.WriteLine("Allocated Resources:");
		if (detail.AllocatedResources.Count == 0)
		{
			Console.WriteLine("  No active allocations.");
		}
		else
		{
			Console.WriteLine($"  {"Name",-16} {"%",4}  {"From",-11} To");
			foreach (var resource in detail.AllocatedResources)
			{
				var start = DateFormatHelper.ParseApiDate(resource.AllocationStartDate);
				var end = DateFormatHelper.ParseApiDate(resource.AllocationEndDate);
				Console.WriteLine(
					$"  {resource.EmployeeName,-16} {resource.AllocationPercentage,3:0}%  " +
					$"{DateFormatHelper.FormatDisplay(start),-11} {DateFormatHelper.FormatDisplay(end)}");
			}
		}

		Console.WriteLine();
		Console.WriteLine("[A] Get AI Risk Summary     [B] Back");
		Console.Write("Enter option: ");
		var option = Console.ReadLine()?.Trim().ToUpperInvariant();
		if (option == "A")
		{
			Console.WriteLine();
			Console.WriteLine("Generating AI summary...");
			var aiResponse = await _aiClient.GetRiskSummaryAsync(projectId);
			if (!aiResponse.Success || aiResponse.Data == null)
			{
				ConsoleHelper.WriteError(aiResponse.Error ?? "Risk summary failed.");
			}
			else
			{
				var summary = aiResponse.Data;
				Console.WriteLine();
				Console.WriteLine(summary.Paragraph ?? summary.Message ?? "No summary available.");
				Console.WriteLine();
				Console.WriteLine($"Note: {summary.Disclaimer}");
			}

			ConsoleHelper.PressEnterToContinue();
		}
	}
}
