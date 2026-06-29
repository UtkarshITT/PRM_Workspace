using FluentValidation;
using PRM.Server.Models.DTOs.Allocations;

namespace PRM.Server.Validators;

public class CreateAllocationValidator : AbstractValidator<CreateAllocationDto>
{
	public CreateAllocationValidator()
	{
		RuleFor(x => x.EmployeeId).GreaterThan(0);
		RuleFor(x => x.ProjectId).GreaterThan(0);
		RuleFor(x => x.AllocationPercentage).InclusiveBetween(1, 100);
		RuleFor(x => x.AllocationStartDate)
			.NotEmpty()
			.Must(BeTodayOrFuture)
			.WithMessage("Allocation start date cannot be in the past.");
		RuleFor(x => x.AllocationEndDate).NotEmpty().GreaterThan(x => x.AllocationStartDate);
	}

	private static bool BeTodayOrFuture(DateOnly date)
	{
		return date >= DateOnly.FromDateTime(DateTime.Today);
	}
}
