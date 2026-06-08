using FluentValidation;
using PRM.Server.Models.DTOs.Projects;

namespace PRM.Server.Validators;

public class CreateMilestoneValidator : AbstractValidator<CreateMilestoneDto>
{
	public CreateMilestoneValidator()
	{
		RuleFor(x => x.MilestoneTitle).NotEmpty().MaximumLength(200);
		RuleFor(x => x.DueDate).NotEmpty();
		RuleFor(x => x.StoryPoints).GreaterThan(0);
		RuleFor(x => x.SortOrder).GreaterThan((short)0);
	}
}
