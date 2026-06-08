using FluentValidation;
using PRM.Server.Constants;
using PRM.Server.Models.DTOs.Projects;

namespace PRM.Server.Validators;

public class UpdateMilestoneStatusValidator : AbstractValidator<UpdateMilestoneStatusDto>
{
	public UpdateMilestoneStatusValidator()
	{
		RuleFor(x => x.MilestoneStatus)
			.NotEmpty()
			.Must(status => MilestoneStatuses.All.Contains(status))
			.WithMessage($"Status must be one of: {string.Join(", ", MilestoneStatuses.All)}.");
	}
}
