using PRM.Server.Models.Entities;

namespace PRM.Server.Helpers;

public static class UtilizationCalculator
{
	public static decimal CalculateCurrentUtilization(IEnumerable<ProjectAllocation> allocations, DateOnly asOfDate)
	{
		return allocations
			.Where(allocation =>
				allocation.AllocationStatus == "ACTIVE"
				&& allocation.AllocationStartDate <= asOfDate
				&& allocation.AllocationEndDate >= asOfDate)
			.Sum(allocation => allocation.AllocationPercentage);
	}

	public static decimal CalculateOverlappingUtilization(
		IEnumerable<ProjectAllocation> allocations,
		DateOnly periodStart,
		DateOnly periodEnd)
	{
		return allocations
			.Where(allocation =>
				allocation.AllocationStatus == "ACTIVE"
				&& PeriodsOverlap(
					allocation.AllocationStartDate,
					allocation.AllocationEndDate,
					periodStart,
					periodEnd))
			.Sum(allocation => allocation.AllocationPercentage);
	}

	public static bool PeriodsOverlap(DateOnly start1, DateOnly end1, DateOnly start2, DateOnly end2) =>
		start1 <= end2 && start2 <= end1;
}
