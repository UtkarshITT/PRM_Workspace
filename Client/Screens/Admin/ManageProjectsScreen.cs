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
			Console.WriteLine("  5. Back");
			Console.WriteLine();
			Console.Write("Enter option: ");

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
				case "5":
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

		if (!TryReadRequired("Project Name        : ", "Project name is required.", out var name))
		{
			return;
		}

		var description = ConsoleHelper.ReadInput("Description         : ");
		if (!TryReadDate("Start Date (DD-MM-YYYY): ", allowPast: false, out var startDate))
		{
			return;
		}

		if (!TryReadDate("End Date (DD-MM-YYYY)  : ", allowPast: false, out var endDate))
		{
			return;
		}

		if (endDate < startDate)
		{
			ConsoleHelper.WriteError("End date cannot be before start date.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var status = PromptProjectStatus(includeCompleted: false);
		if (status == null)
		{
			return;
		}

		if (!TryReadPositiveLong("Manager User ID     : ", "Manager user ID must be a positive number.", out var managerId))
		{
			return;
		}

		if (!TryReadNonNegativeInt("Total Story Points  : ", "Story points cannot be negative.", out var storyPoints))
		{
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

		var project = await GetProjectForUpdateAsync(projectId);
		if (project == null)
		{
			return;
		}

		var currentStartDate = DateOnly.Parse(project.StartDate, CultureInfo.InvariantCulture);
		var currentEndDate = DateOnly.Parse(project.EndDate, CultureInfo.InvariantCulture);

		ConsoleHelper.WriteKeepCurrentHint();
		var nameInput = ConsoleHelper.ReadOptionalUpdateInput("Project Name       ", project.ProjectName);
		var descriptionInput = ConsoleHelper.ReadOptionalUpdateInput("Description        ", project.Description);
		var name = string.IsNullOrWhiteSpace(nameInput) ? project.ProjectName : nameInput;
		var description = ResolveOptionalUpdateValue(descriptionInput, project.Description);

		if (!TryReadUpdateDate("Start Date (DD-MM-YYYY)", currentStartDate, out var startDate))
		{
			return;
		}

		if (!TryReadUpdateDate("End Date (DD-MM-YYYY)  ", currentEndDate, out var endDate))
		{
			return;
		}

		if (endDate < startDate)
		{
			ConsoleHelper.WriteError("End date cannot be before start date.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var status = PromptProjectStatus(includeCompleted: true, currentStatus: project.ProjectStatus);
		if (status == null)
		{
			return;
		}

		if (!TryReadUpdatePositiveLong(
			    "Manager User ID     ",
			    project.ManagerUserId,
			    "Manager user ID must be a positive number.",
			    out var managerId))
		{
			return;
		}

		if (!TryReadUpdateNonNegativeInt(
			    "Total Story Points  ",
			    project.TotalStoryPoints,
			    "Story points cannot be negative.",
			    out var storyPoints))
		{
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

	private async Task<ProjectListItem?> GetProjectForUpdateAsync(long projectId)
	{
		var response = await _adminClient.GetProjectsAsync();
		if (!response.Success || response.Data == null)
		{
			WriteApiError(response);
			return null;
		}

		var project = response.Data.FirstOrDefault(item => item.Id == projectId);
		if (project != null)
		{
			return project;
		}

		ConsoleHelper.WriteError("Project not found.");
		ConsoleHelper.PressEnterToContinue();
		return null;
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
			Console.WriteLine("  3. Back");
			Console.Write("Enter option: ");

			switch (Console.ReadLine()?.Trim())
			{
				case "1":
					await AddMilestoneAsync(projectId, milestones.Count);
					break;
				case "2":
					await UpdateMilestoneStatusAsync(projectId, milestones);
					break;
				case "3":
					running = false;
					break;
			}
		}
	}

	private async Task AddMilestoneAsync(long projectId, int existingCount)
	{
		var title = ConsoleHelper.ReadInput("Milestone Title: ");
		if (!TryReadDate("Due Date (DD-MM-YYYY): ", allowPast: true, out var dueDate))
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

		ConsoleHelper.WriteKeepCurrentHint();
		var status = PromptMilestoneStatus(milestone.MilestoneStatus);
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

	private static bool TryReadDate(string prompt, bool allowPast, out DateOnly date)
	{
		var input = ConsoleHelper.ReadInput(prompt);
		if (DateFormatHelper.TryParseInput(input, out date))
		{
			if (!allowPast && date < DateOnly.FromDateTime(DateTime.Today))
			{
				ConsoleHelper.WriteError("Date cannot be in the past.");
				ConsoleHelper.PressEnterToContinue();
				return false;
			}

			return true;
		}

		ConsoleHelper.WriteError("Invalid date. Use DD-MM-YYYY.");
		ConsoleHelper.PressEnterToContinue();
		date = default;
		return false;
	}

	private static bool TryReadUpdateDate(string label, DateOnly currentDate, out DateOnly date)
	{
		var input = ConsoleHelper.ReadOptionalUpdateInput(label, DateFormatHelper.FormatDisplay(currentDate));
		if (string.IsNullOrWhiteSpace(input))
		{
			date = currentDate;
			return true;
		}

		if (!DateFormatHelper.TryParseInput(input, out date))
		{
			ConsoleHelper.WriteError("Invalid date. Use DD-MM-YYYY.");
			ConsoleHelper.PressEnterToContinue();
			return false;
		}

		if (date < DateOnly.FromDateTime(DateTime.Today))
		{
			ConsoleHelper.WriteError("Date cannot be in the past.");
			ConsoleHelper.PressEnterToContinue();
			return false;
		}

		return true;
	}

	private static bool TryReadRequired(string prompt, string error, out string value)
	{
		value = ConsoleHelper.ReadInput(prompt);
		if (!string.IsNullOrWhiteSpace(value))
		{
			return true;
		}

		ConsoleHelper.WriteError(error);
		ConsoleHelper.PressEnterToContinue();
		return false;
	}

	private static bool TryReadPositiveLong(string prompt, string error, out long value)
	{
		if (long.TryParse(ConsoleHelper.ReadInput(prompt), out value) && value > 0)
		{
			return true;
		}

		ConsoleHelper.WriteError(error);
		ConsoleHelper.PressEnterToContinue();
		return false;
	}

	private static bool TryReadUpdatePositiveLong(string label, long currentValue, string error, out long value)
	{
		var input = ConsoleHelper.ReadOptionalUpdateInput(label, currentValue.ToString(CultureInfo.InvariantCulture));
		if (string.IsNullOrWhiteSpace(input))
		{
			value = currentValue;
			return true;
		}

		if (long.TryParse(input, out value) && value > 0)
		{
			return true;
		}

		ConsoleHelper.WriteError(error);
		ConsoleHelper.PressEnterToContinue();
		return false;
	}

	private static bool TryReadNonNegativeInt(string prompt, string error, out int value)
	{
		if (int.TryParse(ConsoleHelper.ReadInput(prompt), out value) && value >= 0)
		{
			return true;
		}

		ConsoleHelper.WriteError(error);
		ConsoleHelper.PressEnterToContinue();
		return false;
	}

	private static bool TryReadUpdateNonNegativeInt(string label, int currentValue, string error, out int value)
	{
		var input = ConsoleHelper.ReadOptionalUpdateInput(label, currentValue.ToString(CultureInfo.InvariantCulture));
		if (string.IsNullOrWhiteSpace(input))
		{
			value = currentValue;
			return true;
		}

		if (int.TryParse(input, out value) && value >= 0)
		{
			return true;
		}

		ConsoleHelper.WriteError(error);
		ConsoleHelper.PressEnterToContinue();
		return false;
	}

	private static string? PromptProjectStatus(bool includeCompleted, string? currentStatus = null)
	{
		Console.WriteLine("Status: (1) PLANNED  (2) ACTIVE  (3) ON_HOLD" + (includeCompleted ? "  (4) COMPLETED" : ""));
		Console.Write(string.IsNullOrWhiteSpace(currentStatus) ? "Enter choice: " : $"Enter choice [{currentStatus}]: ");

		var input = Console.ReadLine()?.Trim();
		if (string.IsNullOrWhiteSpace(input) && !string.IsNullOrWhiteSpace(currentStatus))
		{
			return currentStatus;
		}

		var status = input switch
		{
			"1" => "PLANNED",
			"2" => "ACTIVE",
			"3" => "ON_HOLD",
			"4" when includeCompleted => "COMPLETED",
			_ => null
		};

		if (status != null)
		{
			return status;
		}

		ConsoleHelper.WriteError("Invalid project status selection.");
		ConsoleHelper.PressEnterToContinue();
		return null;
	}

	private static string? PromptMilestoneStatus(string? currentStatus = null)
	{
		Console.WriteLine("New Status: (1) NOT_STARTED  (2) IN_PROGRESS  (3) DONE");
		Console.Write(string.IsNullOrWhiteSpace(currentStatus) ? "Enter choice: " : $"Enter choice [{currentStatus}]: ");

		var input = Console.ReadLine()?.Trim();
		if (string.IsNullOrWhiteSpace(input) && !string.IsNullOrWhiteSpace(currentStatus))
		{
			return currentStatus;
		}

		var status = input switch
		{
			"1" => "NOT_STARTED",
			"2" => "IN_PROGRESS",
			"3" => "DONE",
			_ => null
		};

		if (status != null)
		{
			return status;
		}

		ConsoleHelper.WriteError("Invalid milestone status selection.");
		ConsoleHelper.PressEnterToContinue();
		return null;
	}

	private static string? ResolveOptionalUpdateValue(string input, string? currentValue)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			return currentValue;
		}

		return input == "-" ? null : input;
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
