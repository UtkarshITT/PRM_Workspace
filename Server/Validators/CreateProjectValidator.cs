using FluentValidation;
using PRM.Server.Constants;
using PRM.Server.Models.DTOs.Projects;

namespace PRM.Server.Validators;

public class CreateProjectValidator : AbstractValidator<CreateProjectDto>
{
	public CreateProjectValidator()
	{
		RuleFor(x => x.ProjectName).NotEmpty().MaximumLength(200);
		RuleFor(x => x.StartDate)
			.NotEmpty()
			.Must(BeTodayOrFuture)
			.WithMessage("Project start date cannot be in the past.");
		RuleFor(x => x.EndDate).NotEmpty().GreaterThanOrEqualTo(x => x.StartDate);
		RuleFor(x => x.ProjectStatus)
			.Must(status => ProjectStatuses.All.Contains(status))
			.WithMessage($"Status must be one of: {string.Join(", ", ProjectStatuses.All)}.");
		RuleFor(x => x.ManagerUserId).GreaterThan(0);
		RuleFor(x => x.TotalStoryPoints).GreaterThanOrEqualTo(0);
	}

	private static bool BeTodayOrFuture(DateOnly date)
	{
		return date >= DateOnly.FromDateTime(DateTime.Today);
	}
}
