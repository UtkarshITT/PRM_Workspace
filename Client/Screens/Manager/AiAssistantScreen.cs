using PRM.Client.Helpers;
using PRM.Client.HttpClients;
using PRM.Client.Models.Manager;

namespace PRM.Client.Screens.Manager;

public class AiAssistantScreen
{
	private readonly AiClient _aiClient;
	private readonly ManagerClient _managerClient;
	private readonly AllocateResourceScreen _allocateResourceScreen;

	public AiAssistantScreen(
		AiClient aiClient,
		ManagerClient managerClient,
		AllocateResourceScreen allocateResourceScreen)
	{
		_aiClient = aiClient;
		_managerClient = managerClient;
		_allocateResourceScreen = allocateResourceScreen;
	}

	public async Task ShowAsync()
	{
		var running = true;

		while (running)
		{
			Console.Clear();
			ConsoleHelper.WriteHeader("AI Assistant");
			Console.WriteLine("  1. Skill Match");
			Console.WriteLine("  2. Risk Summary");
			Console.WriteLine("  3. Team Builder");
			Console.WriteLine("  4. Back");
			Console.WriteLine();
			Console.Write("Enter option: ");

			switch (Console.ReadLine()?.Trim())
			{
				case "1":
					await ShowSkillMatchAsync();
					break;
				case "2":
					await ShowRiskSummaryAsync();
					break;
				case "3":
					await ShowTeamBuilderAsync();
					break;
				case "4":
					running = false;
					break;
				default:
					ConsoleHelper.WriteError("Invalid option.");
					ConsoleHelper.PressEnterToContinue();
					break;
			}
		}
	}

	private async Task ShowSkillMatchAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Skill Match");
		Console.WriteLine("Describe your project requirement in plain English:");
		Console.Write("> ");
		var requirement = Console.ReadLine()?.Trim();

		if (string.IsNullOrWhiteSpace(requirement))
		{
			ConsoleHelper.WriteError("Requirement cannot be empty.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		Console.WriteLine();
		Console.WriteLine("Searching... (calling AI)");
		var response = await _aiClient.GetSkillMatchAsync(requirement);

		if (!response.Success || response.Data == null)
		{
			ConsoleHelper.WriteError(response.Error ?? "Skill match failed.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		DisplaySkillMatchResults(response.Data);

		Console.WriteLine();
		Console.WriteLine("[A] Go to Allocate Resource     [B] Back");
		Console.Write("Enter option: ");
		if (Console.ReadLine()?.Trim().ToUpperInvariant() == "A")
		{
			await _allocateResourceScreen.ShowAsync();
		}
	}

	private async Task ShowRiskSummaryAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Risk Summary");

		var projectsResponse = await _managerClient.GetMyProjectsAsync();
		if (!projectsResponse.Success || projectsResponse.Data == null || projectsResponse.Data.Count == 0)
		{
			ConsoleHelper.WriteError(projectsResponse.Error ?? "No projects found.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var projects = projectsResponse.Data;
		for (var index = 0; index < projects.Count; index++)
		{
			var project = projects[index];
			var icon = HealthStatusHelper.ToDisplayIcon(project.HealthStatus);
			var label = HealthStatusHelper.ToDisplayLabel(project.HealthStatus);
			Console.WriteLine($"  {index + 1}.  {project.ProjectName,-16} {icon} {label}");
		}

		Console.WriteLine();
		Console.Write("Enter project number (0 = Back): ");
		if (!int.TryParse(Console.ReadLine()?.Trim(), out var selection) || selection == 0)
		{
			return;
		}

		if (selection < 1 || selection > projects.Count)
		{
			ConsoleHelper.WriteError("Invalid project number.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var projectId = projects[selection - 1].Id;
		Console.WriteLine();
		Console.WriteLine("Generating AI summary...");
		var response = await _aiClient.GetRiskSummaryAsync(projectId);

		if (!response.Success || response.Data == null)
		{
			ConsoleHelper.WriteError(response.Error ?? "Risk summary failed.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		DisplayRiskSummary(response.Data);
		ConsoleHelper.PressEnterToContinue();
	}

	private async Task ShowTeamBuilderAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Team Builder");
		Console.WriteLine("Describe the team you need in one line.");
		Console.WriteLine("Example: I need 4 developers for Python, 1 QA, and 1 DevOps.");
		Console.WriteLine("Only employees with 100% availability will be considered.");
		Console.WriteLine();
		Console.Write("> ");
		var prompt = Console.ReadLine()?.Trim();
		if (string.IsNullOrWhiteSpace(prompt))
		{
			ConsoleHelper.WriteError("Team request is required.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		Console.WriteLine();
		Console.WriteLine("Building team... (calling AI)");
		var response = await _aiClient.BuildTeamAsync(new TeamBuilderRequest { Prompt = prompt });

		if (!response.Success || response.Data == null)
		{
			ConsoleHelper.WriteError(response.Error ?? "Team builder failed.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		DisplayTeamBuilderResults(response.Data);
		ConsoleHelper.PressEnterToContinue();
	}

	private static void DisplaySkillMatchResults(AiSkillMatchResponse data)
	{
		Console.WriteLine();
		if (!string.IsNullOrWhiteSpace(data.Message))
		{
			Console.WriteLine(data.Message);
		}

		if (!string.IsNullOrWhiteSpace(data.Summary))
		{
			Console.WriteLine(data.Summary);
		}

		WriteGapAnalysis(data.GapAnalysis);

		if (data.Candidates.Count == 0)
		{
			Console.WriteLine("No matches found.");
			return;
		}

		Console.WriteLine("Results:");
		foreach (var candidate in data.Candidates.OrderBy(item => item.Rank))
		{
			Console.WriteLine($"  {candidate.Rank}.  {candidate.FullName} (ID {candidate.EmployeeId})");
			Console.WriteLine($"      Reason: {candidate.Reason}");
			Console.WriteLine($"      Availability: {candidate.AvailabilityPercent:0}%");
		}

		if (data.AiGenerated && !string.IsNullOrWhiteSpace(data.Disclaimer))
		{
			Console.WriteLine();
			Console.WriteLine($"Note: {data.Disclaimer}");
		}
	}

	private static void DisplayRiskSummary(AiRiskSummaryResponse data)
	{
		Console.WriteLine();
		if (!string.IsNullOrWhiteSpace(data.Message))
		{
			Console.WriteLine(data.Message);
			Console.WriteLine();
		}

		if (!string.IsNullOrWhiteSpace(data.ProjectName))
		{
			Console.WriteLine($"── AI Risk Summary — {data.ProjectName} ──");
			Console.WriteLine();
		}

		Console.WriteLine(data.Paragraph ?? "No summary available.");
		if (data.AiGenerated && !string.IsNullOrWhiteSpace(data.Disclaimer))
		{
			Console.WriteLine();
			Console.WriteLine($"Note: {data.Disclaimer}");
		}
	}

	private static void DisplayTeamBuilderResults(TeamBuilderResponse data)
	{
		Console.WriteLine();
		if (!string.IsNullOrWhiteSpace(data.Message))
		{
			Console.WriteLine(data.Message);
			Console.WriteLine();
		}

		if (!string.IsNullOrWhiteSpace(data.Summary))
		{
			Console.WriteLine(data.Summary);
			Console.WriteLine();
		}

		WriteGapAnalysis(data.GapAnalysis);

		foreach (var role in data.Roles)
		{
			var headcount = role.Headcount > 0 ? $" (needed: {role.Headcount})" : string.Empty;
			Console.WriteLine($"── Role: {role.RoleTitle}{headcount} ──");
			if (role.Matches.Count == 0)
			{
				Console.WriteLine("  No 100% available candidate found for this role.");
				Console.WriteLine("  Suggested action: review gap analysis for hiring or training options.");
				continue;
			}

			foreach (var match in role.Matches)
			{
				Console.WriteLine($"  {match.Rank}. {match.FullName} (ID {match.EmployeeId}) — {match.Reason}");
				Console.WriteLine($"     Availability: {match.AvailabilityPercent:0}%");
			}

			Console.WriteLine();
		}

		if (data.AiGenerated && !string.IsNullOrWhiteSpace(data.Disclaimer))
		{
			Console.WriteLine($"Note: {data.Disclaimer}");
		}
	}

	private static void WriteGapAnalysis(IReadOnlyList<string> gapAnalysis)
	{
		if (gapAnalysis.Count == 0)
		{
			return;
		}

		Console.WriteLine("Gap analysis:");
		foreach (var gap in gapAnalysis)
		{
			Console.WriteLine($"  - {gap}");
		}

		Console.WriteLine();
	}

}
