using FluentValidation;
using PRM.Server.Constants;
using PRM.Server.Models.DTOs.Employees;

namespace PRM.Server.Validators;

public class UpdateSkillProficiencyValidator : AbstractValidator<UpdateSkillProficiencyDto>
{
	public UpdateSkillProficiencyValidator()
	{
		RuleFor(x => x.ProficiencyLevel)
			.NotEmpty()
			.Must(level => ProficiencyLevels.All.Contains(level))
			.WithMessage($"Proficiency must be one of: {string.Join(", ", ProficiencyLevels.All)}.");
	}
}
