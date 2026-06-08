using System.Globalization;
using PRM.Client.Helpers;
using PRM.Client.HttpClients;
using PRM.Client.Models.Admin;

namespace PRM.Client.Screens.Admin;

public class ManageProjectsScreen
{
	private readonly AdminClient _adminClient;

	public ManageProjectsScreen(AdminClient adminClient)
	{
		_adminClient = adminClient;
	}

	public async Task ShowAsync()
	{
		var running = true;

		while (running)
		{
			Console.Clear();
			ConsoleHelper.WriteHeader("Manage Projects");
			Console.WriteLine("  1. Create Project");
			Console.WriteLine("  2. View All Projects");
			Console.WriteLine("  3. Update Project Details");
			Console.WriteLine("  4. Manage Milestones");
			Console.WriteLine("  0. Back");
			Console.WriteLine();
			Console.Write("Select option: ");

			switch (Console.ReadLine()?.Trim())
			{
				case "1":
					await CreateProjectAsync();
					break;
				case "2":
					await ViewProjectsAsync();
					break;
				case "3":
					await UpdateProjectAsync();
					break;
				case "4":
					await ManageMilestonesAsync();
					break;
				case "0":
					running = false;
					break;
				default:
					ConsoleHelper.WriteError("Invalid option.");
					ConsoleHelper.PressEnterToContinue();
					break;
			}
		}
	}

	private async Task CreateProjectAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Create Project");

		var name = ConsoleHelper.ReadInput("Project Name        : ");
		var description = ConsoleHelper.ReadInput("Description         : ");
		if (!TryReadDate("Start Date (DD-MM-YYYY): ", out var startDate))
		{
			return;
		}

		if (!TryReadDate("End Date (DD-MM-YYYY)  : ", out var endDate))
		{
			return;
		}

		var status = PromptProjectStatus(includeCompleted: false);
		if (status == null)
		{
			return;
		}

		if (!long.TryParse(ConsoleHelper.ReadInput("Manager User ID     : "), out var managerId))
		{
			ConsoleHelper.WriteError("Invalid manager user ID.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		if (!int.TryParse(ConsoleHelper.ReadInput("Total Story Points  : "), out var storyPoints))
		{
			ConsoleHelper.WriteError("Invalid story points.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var response = await _adminClient.CreateProjectAsync(new CreateProjectRequest
		{
			ProjectName = name,
			Description = string.IsNullOrWhiteSpace(description) ? null : description,
			StartDate = DateFormatHelper.ToApiDate(startDate),
			EndDate = DateFormatHelper.ToApiDate(endDate),
			ProjectStatus = status,
			ManagerUserId = managerId,
			TotalStoryPoints = storyPoints
		});

		if (response.Success && response.Data != null)
		{
			ConsoleHelper.WriteSuccess($"Project created ({response.Data.ProjectCode}).");
		}
		else
		{
			WriteApiError(response);
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private async Task ViewProjectsAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("All Projects");

		var response = await _adminClient.GetProjectsAsync();
		if (!response.Success || response.Data == null)
		{
			WriteApiError(response);
			return;
		}

		Console.WriteLine($"{"ID",-6}{"Name",-18}{"Manager",-14}{"End Date",-12}{"Status",-10}SP Done/Total");
		Console.WriteLine(new string('-', 80));

		foreach (var project in response.Data)
		{
			var endDate = DateOnly.Parse(project.EndDate, CultureInfo.InvariantCulture);
			Console.WriteLine(
				$"{project.Id,-6}{project.ProjectName,-18}{project.ManagerName,-14}{DateFormatHelper.FormatDisplay(endDate),-12}{project.ProjectStatus,-10}{project.StoryPointsDone} / {project.TotalStoryPoints}");
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private async Task UpdateProjectAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Update Project Details");

		if (!long.TryParse(ConsoleHelper.ReadInput("Project ID: "), out var projectId))
		{
			ConsoleHelper.WriteError("Invalid project ID.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var name = ConsoleHelper.ReadInput("Project Name        : ");
		var description = ConsoleHelper.ReadInput("Description         : ");
		if (!TryReadDate("Start Date (DD-MM-YYYY): ", out var startDate))
		{
			return;
		}

		if (!TryReadDate("End Date (DD-MM-YYYY)  : ", out var endDate))
		{
			return;
		}

		var status = PromptProjectStatus(includeCompleted: true);
		if (status == null)
		{
			return;
		}

		if (!long.TryParse(ConsoleHelper.ReadInput("Manager User ID     : "), out var managerId))
		{
			ConsoleHelper.WriteError("Invalid manager user ID.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		if (!int.TryParse(ConsoleHelper.ReadInput("Total Story Points  : "), out var storyPoints))
		{
			ConsoleHelper.WriteError("Invalid story points.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var response = await _adminClient.UpdateProjectAsync(projectId, new UpdateProjectRequest
		{
			ProjectName = name,
			Description = string.IsNullOrWhiteSpace(description) ? null : description,
			StartDate = DateFormatHelper.ToApiDate(startDate),
			EndDate = DateFormatHelper.ToApiDate(endDate),
			ProjectStatus = status,
			ManagerUserId = managerId,
			TotalStoryPoints = storyPoints
		});

		if (response.Success)
		{
			ConsoleHelper.WriteSuccess("Project updated.");
		}
		else
		{
			WriteApiError(response);
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private async Task ManageMilestonesAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Manage Milestones");

		if (!long.TryParse(ConsoleHelper.ReadInput("Project ID: "), out var projectId))
		{
			ConsoleHelper.WriteError("Invalid project ID.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var running = true;
		while (running)
		{
			var milestonesResponse = await _adminClient.GetMilestonesAsync(projectId);
			Console.Clear();
			ConsoleHelper.WriteHeader($"Milestones — Project {projectId}");

			if (!milestonesResponse.Success || milestonesResponse.Data == null)
			{
				WriteApiError(milestonesResponse);
				return;
			}

			var milestones = milestonesResponse.Data;
			Console.WriteLine($"{"#",-4}{"Title",-22}{"Due Date",-12}{"Story Pts",-10}Status");
			Console.WriteLine(new string('-', 70));

			var completed = 0;
			foreach (var milestone in milestones)
			{
				var dueDate = DateOnly.Parse(milestone.DueDate, CultureInfo.InvariantCulture);
				Console.WriteLine(
					$"{milestone.SortOrder,-4}{milestone.MilestoneTitle,-22}{DateFormatHelper.FormatDisplay(dueDate),-12}{milestone.StoryPoints,-10}{milestone.MilestoneStatus}");
				if (milestone.MilestoneStatus == "DONE")
				{
					completed += milestone.StoryPoints;
				}
			}

			var total = milestones.Sum(item => item.StoryPoints);
			Console.WriteLine();
			Console.WriteLine($"Total: {total} SP   |   Completed: {completed} SP   |   Remaining: {total - completed} SP");
			Console.WriteLine();
			Console.WriteLine("  1. Add Milestone");
			Console.WriteLine("  2. Update Milestone Status");
			Console.WriteLine("  0. Back");
			Console.Write("Select option: ");

			switch (Console.ReadLine()?.Trim())
			{
				case "1":
					await AddMilestoneAsync(projectId, milestones.Count);
					break;
				case "2":
					await UpdateMilestoneStatusAsync(projectId, milestones);
					break;
				case "0":
					running = false;
					break;
			}
		}
	}

	private async Task AddMilestoneAsync(long projectId, int existingCount)
	{
		var title = ConsoleHelper.ReadInput("Milestone Title: ");
		if (!TryReadDate("Due Date (DD-MM-YYYY): ", out var dueDate))
		{
			return;
		}

		if (!int.TryParse(ConsoleHelper.ReadInput("Story Points   : "), out var storyPoints))
		{
			ConsoleHelper.WriteError("Invalid story points.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var response = await _adminClient.AddMilestoneAsync(projectId, new CreateMilestoneRequest
		{
			MilestoneTitle = title,
			DueDate = DateFormatHelper.ToApiDate(dueDate),
			StoryPoints = storyPoints,
			SortOrder = (short)(existingCount + 1)
		});

		if (response.Success)
		{
			ConsoleHelper.WriteSuccess("Milestone added.");
		}
		else
		{
			WriteApiError(response);
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private async Task UpdateMilestoneStatusAsync(long projectId, List<MilestoneListItem> milestones)
	{
		if (!short.TryParse(ConsoleHelper.ReadInput("Milestone # : "), out var sortOrder))
		{
			ConsoleHelper.WriteError("Invalid milestone number.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var milestone = milestones.FirstOrDefault(item => item.SortOrder == sortOrder);
		if (milestone == null)
		{
			ConsoleHelper.WriteError("Milestone not found.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var status = PromptMilestoneStatus();
		if (status == null)
		{
			return;
		}

		var response = await _adminClient.UpdateMilestoneStatusAsync(projectId, milestone.Id, new UpdateMilestoneStatusRequest
		{
			MilestoneStatus = status
		});

		if (response.Success)
		{
			ConsoleHelper.WriteSuccess("Milestone updated.");
		}
		else
		{
			WriteApiError(response);
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private static bool TryReadDate(string prompt, out DateOnly date)
	{
		var input = ConsoleHelper.ReadInput(prompt);
		if (DateFormatHelper.TryParseInput(input, out date))
		{
			return true;
		}

		ConsoleHelper.WriteError("Invalid date. Use DD-MM-YYYY.");
		ConsoleHelper.PressEnterToContinue();
		date = default;
		return false;
	}

	private static string? PromptProjectStatus(bool includeCompleted)
	{
		Console.WriteLine("Status: (1) PLANNED  (2) ACTIVE  (3) ON_HOLD" + (includeCompleted ? "  (4) COMPLETED" : ""));
		Console.Write("Enter choice: ");

		return Console.ReadLine()?.Trim() switch
		{
			"1" => "PLANNED",
			"2" => "ACTIVE",
			"3" => "ON_HOLD",
			"4" when includeCompleted => "COMPLETED",
			_ => null
		};
	}

	private static string? PromptMilestoneStatus()
	{
		Console.WriteLine("New Status: (1) NOT_STARTED  (2) IN_PROGRESS  (3) DONE");
		Console.Write("Enter choice: ");

		return Console.ReadLine()?.Trim() switch
		{
			"1" => "NOT_STARTED",
			"2" => "IN_PROGRESS",
			"3" => "DONE",
			_ => null
		};
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
