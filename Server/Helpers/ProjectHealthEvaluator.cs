using PRM.Server.Constants;
using PRM.Server.Models.DTOs.Projects;
using PRM.Server.Models.Entities;

namespace PRM.Server.Helpers;

public static class ProjectHealthEvaluator
{
	public static IReadOnlyList<ProjectRiskFlagDto> EvaluateRiskFlags(
		Project project,
		IReadOnlyList<ProjectAllocation> activeAllocations,
		IReadOnlyList<(string EmployeeName, decimal HoursLogged, decimal ExpectedHours)> lastWeekHours)
	{
		var flags = new List<ProjectRiskFlagDto>();
		var today = DateOnly.FromDateTime(DateTime.UtcNow);

		foreach (var milestone in project.Milestones
			         .Where(milestone => milestone.DueDate < today && milestone.MilestoneStatus != MilestoneStatuses.Done)
			         .OrderBy(milestone => milestone.DueDate))
		{
			var daysOverdue = today.DayNumber - milestone.DueDate.DayNumber;
			flags.Add(new ProjectRiskFlagDto
			{
				IsPositive = false,
				Message = $"{milestone.MilestoneTitle} milestone is {daysOverdue} day(s) overdue"
			});
		}

		foreach (var (employeeName, hoursLogged, expectedHours) in lastWeekHours)
		{
			if (expectedHours > 0 && hoursLogged < expectedHours * 0.6m)
			{
				flags.Add(new ProjectRiskFlagDto
				{
					IsPositive = false,
					Message =
						$"{employeeName} logged only {hoursLogged:0} hrs last week (expected {expectedHours:0} hrs)"
				});
			}
		}

		var daysUntilEnd = project.EndDate.DayNumber - today.DayNumber;
		if (daysUntilEnd is >= 0 and < 28
		    && project.Milestones.Any(milestone => milestone.MilestoneStatus != MilestoneStatuses.Done))
		{
			flags.Add(new ProjectRiskFlagDto
			{
				IsPositive = false,
				Message = $"Project deadline is in {daysUntilEnd} day(s) with pending milestones"
			});
		}

		if (activeAllocations.Count > 0)
		{
			var overAllocatedEmployees = activeAllocations
				.GroupBy(allocation => allocation.EmployeeId)
				.Where(group => group.Sum(allocation => allocation.AllocationPercentage) > 100)
				.ToList();

			if (overAllocatedEmployees.Count == 0)
			{
				flags.Add(new ProjectRiskFlagDto
				{
					IsPositive = true,
					Message = "Resources are correctly allocated"
				});
			}
		}

		return flags;
	}
}
