using FluentValidation;
using PRM.Server.Constants;
using PRM.Server.Models.DTOs.Employees;

namespace PRM.Server.Validators;

public class AddSkillValidator : AbstractValidator<AddSkillDto>
{
	public AddSkillValidator()
	{
		RuleFor(x => x.SkillName).NotEmpty().MaximumLength(100);
		RuleFor(x => x.Category)
			.NotEmpty()
			.Must(category => SkillCategories.All.Contains(category))
			.WithMessage($"Category must be one of: {string.Join(", ", SkillCategories.All)}.");
		RuleFor(x => x.ProficiencyLevel)
			.NotEmpty()
			.Must(level => ProficiencyLevels.All.Contains(level))
			.WithMessage($"Proficiency must be one of: {string.Join(", ", ProficiencyLevels.All)}.");
	}
}
