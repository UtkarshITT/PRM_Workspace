using FluentValidation;
using PRM.Server.Constants;
using PRM.Server.Models.DTOs.Projects;

namespace PRM.Server.Validators;

public class CreateProjectValidator : AbstractValidator<CreateProjectDto>
{
	public CreateProjectValidator()
	{
		RuleFor(x => x.ProjectName).NotEmpty().MaximumLength(200);
		RuleFor(x => x.StartDate).NotEmpty();
		RuleFor(x => x.EndDate).NotEmpty().GreaterThanOrEqualTo(x => x.StartDate);
		RuleFor(x => x.ProjectStatus)
			.Must(status => ProjectStatuses.All.Contains(status))
			.WithMessage($"Status must be one of: {string.Join(", ", ProjectStatuses.All)}.");
		RuleFor(x => x.ManagerUserId).GreaterThan(0);
		RuleFor(x => x.TotalStoryPoints).GreaterThanOrEqualTo(0);
	}
}
