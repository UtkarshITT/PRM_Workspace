using PRM.Server.Constants;
using PRM.Server.Models.Entities;

namespace PRM.Server.Helpers;

public static class HealthStatusCalculator
{
	private const decimal LowHoursThreshold = 0.6m;
	private const int ApproachingDeadlineDays = 28;

	public static string FromFlagCount(int flagCount) =>
		flagCount switch
		{
			0 => "GREEN",
			1 => "AMBER",
			_ => "RED"
		};

	public static int CountHealthFlags(
		Project project,
		decimal loggedHoursLastWeek,
		decimal expectedHoursLastWeek,
		DateOnly today)
	{
		var flags = 0;

		if (project.Milestones.Any(milestone =>
			    milestone.DueDate < today && milestone.MilestoneStatus != MilestoneStatuses.Done))
		{
			flags++;
		}

		if (expectedHoursLastWeek > 0 && loggedHoursLastWeek < expectedHoursLastWeek * LowHoursThreshold)
		{
			flags++;
		}

		var daysUntilEnd = project.EndDate.DayNumber - today.DayNumber;
		if (daysUntilEnd is >= 0 and < ApproachingDeadlineDays
		    && project.Milestones.Any(milestone => milestone.MilestoneStatus != MilestoneStatuses.Done))
		{
			flags++;
		}

		return flags;
	}

	public static decimal CalculateExpectedWeeklyHours(
		IEnumerable<ProjectAllocation> allocations,
		DateOnly weekStart,
		DateOnly weekEnd,
		decimal maxWeeklyHours)
	{
		return allocations
			.Where(allocation =>
				allocation.AllocationStatus == "ACTIVE"
				&& UtilizationCalculator.PeriodsOverlap(
					allocation.AllocationStartDate,
					allocation.AllocationEndDate,
					weekStart,
					weekEnd))
			.Sum(allocation => allocation.AllocationPercentage / 100m * maxWeeklyHours);
	}
}
