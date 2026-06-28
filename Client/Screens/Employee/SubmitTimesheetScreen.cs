using PRM.Client.Helpers;
using PRM.Client.HttpClients;
using PRM.Client.Models.Employee;

namespace PRM.Client.Screens.Employee;

public class SubmitTimesheetScreen
{
	private readonly EmployeeClient _employeeClient;

	public SubmitTimesheetScreen(EmployeeClient employeeClient)
	{
		_employeeClient = employeeClient;
	}

	public async Task ShowAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Submit Timesheet");
		Console.WriteLine($"Employee  : {SessionStore.FullName}");

		var today = DateOnly.FromDateTime(DateTime.Today);
		var defaultWeek = DateFormatHelper.GetLastCompletedWeekStart(today);
		var weekInput = ConsoleHelper.ReadInput(
			$"Week Start (DD-MM-YYYY) or Enter for {DateFormatHelper.FormatDisplay(defaultWeek)}: ");

		var weekStart = string.IsNullOrWhiteSpace(weekInput)
			? defaultWeek
			: DateFormatHelper.TryParseInput(weekInput, out var parsed)
				? parsed
				: defaultWeek;

		if (!string.IsNullOrWhiteSpace(weekInput) && !DateFormatHelper.TryParseInput(weekInput, out _))
		{
			ConsoleHelper.WriteError("Invalid date format.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		Console.WriteLine();
		Console.WriteLine("Checking your active allocations for this week...");

		var allocationsResponse = await _employeeClient.GetMyAllocationsAsync(DateFormatHelper.ToApiDate(weekStart));
		if (!allocationsResponse.Success || allocationsResponse.Data == null)
		{
			ConsoleHelper.WriteError(allocationsResponse.Error ?? "Failed to load allocations.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var allocations = allocationsResponse.Data.Allocations;
		if (allocations.Count == 0)
		{
			ConsoleHelper.WriteError("No active allocations found for this week.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var tagsResponse = await _employeeClient.GetActivityTagsAsync();
		if (!tagsResponse.Success || tagsResponse.Data == null || tagsResponse.Data.Count == 0)
		{
			ConsoleHelper.WriteError(tagsResponse.Error ?? "Failed to load activity tags.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var tags = tagsResponse.Data;
		var otherTag = tags.FirstOrDefault(tag => tag.TagCode == "OTHER");
		var lineItems = new List<TimesheetLineItemRequest>();
		var maxWeeklyHours = allocationsResponse.Data.MaxWeeklyHours;

		for (var index = 0; index < allocations.Count; index++)
		{
			var allocation = allocations[index];
			Console.WriteLine();
			Console.WriteLine(new string('─', 46));
			Console.WriteLine($"PROJECT {index + 1} OF {allocations.Count} — {allocation.ProjectName}");
			var expectedHours = allocation.AllocationPercentage / 100m * maxWeeklyHours;
			Console.WriteLine($"  Allocation: {allocation.AllocationPercentage:0}%   |   Expected: {expectedHours:0} hrs max");
			Console.WriteLine(new string('─', 46));

			if (!decimal.TryParse(ConsoleHelper.ReadInput("Hours worked this week: "), out var hours) || hours < 0)
			{
				ConsoleHelper.WriteError("Invalid hours.");
				ConsoleHelper.PressEnterToContinue();
				return;
			}

			if (hours == 0)
			{
				continue;
			}

			Console.WriteLine();
			Console.WriteLine("What did you work on? Select activity tags:");
			for (var tagIndex = 0; tagIndex < tags.Count; tagIndex++)
			{
				Console.WriteLine($"  {tagIndex + 1,2}.  {tags[tagIndex].TagName}");
			}

			var tagInput = ConsoleHelper.ReadInput("Select tags (comma-separated numbers): ");
			var selectedTags = ParseTagSelection(tagInput, tags);
			if (selectedTags.Count == 0)
			{
				ConsoleHelper.WriteError("At least one activity tag is required.");
				ConsoleHelper.PressEnterToContinue();
				return;
			}

			string? customTagText = null;
			if (otherTag != null && selectedTags.Any(tag => tag.Id == otherTag.Id))
			{
				customTagText = ConsoleHelper.ReadInput("Other — describe activity: ");
				if (string.IsNullOrWhiteSpace(customTagText))
				{
					ConsoleHelper.WriteError("Custom tag text is required when 'Other' is selected.");
					ConsoleHelper.PressEnterToContinue();
					return;
				}
			}

			lineItems.Add(new TimesheetLineItemRequest
			{
				ProjectId = allocation.ProjectId,
				HoursLogged = hours,
				ActivityTagIds = selectedTags.Select(tag => tag.Id).ToList(),
				CustomTagText = customTagText
			});
		}

		if (lineItems.Count == 0)
		{
			ConsoleHelper.WriteError("At least one project with hours is required.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var totalHours = lineItems.Sum(item => item.HoursLogged);
		Console.WriteLine();
		Console.WriteLine(new string('─', 46));
		Console.WriteLine("SUMMARY");
		foreach (var item in lineItems)
		{
			var allocation = allocations.First(a => a.ProjectId == item.ProjectId);
			var tagNames = tags.Where(tag => item.ActivityTagIds.Contains(tag.Id)).Select(tag => tag.TagName);
			Console.WriteLine($"  {allocation.ProjectName,-16} {item.HoursLogged,4:0} hrs    [{string.Join(", ", tagNames)}]");
		}

		Console.WriteLine($"  {"Total",-16} {totalHours,4:0} hrs / {maxWeeklyHours} hrs max");
		Console.WriteLine(new string('─', 46));

		var confirm = ConsoleHelper.ReadInput("Submit timesheet (Y/N): ");
		if (!confirm.Equals("Y", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		var submitResponse = await _employeeClient.SubmitTimesheetAsync(new SubmitTimesheetRequest
		{
			WeekStartDate = DateFormatHelper.ToApiDate(weekStart),
			LineItems = lineItems,
			Remarks = null
		});

		if (submitResponse.Success && submitResponse.Data != null)
		{
			ConsoleHelper.WriteSuccess(
				$"Timesheet submitted successfully. Status: {submitResponse.Data.Status}");
		}
		else
		{
			ConsoleHelper.WriteError(submitResponse.Error ?? "Failed to submit timesheet.");
			if (submitResponse.Details.Count > 0)
			{
				foreach (var detail in submitResponse.Details)
				{
					Console.WriteLine($"  - {detail}");
				}
			}
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private static List<ActivityTagItem> ParseTagSelection(string input, IReadOnlyList<ActivityTagItem> tags)
	{
		var selected = new List<ActivityTagItem>();
		var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		foreach (var part in parts)
		{
			if (!int.TryParse(part, out var number) || number < 1 || number > tags.Count)
			{
				return [];
			}

			selected.Add(tags[number - 1]);
		}

		return selected.DistinctBy(tag => tag.Id).ToList();
	}
}
