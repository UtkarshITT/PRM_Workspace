using FluentValidation;
using PRM.Server.Models.DTOs.Ai;

namespace PRM.Server.Validators;

public class TeamBuilderRequestValidator : AbstractValidator<TeamBuilderRequestDto>
{
	public TeamBuilderRequestValidator()
	{
		RuleFor(request => request)
			.Must(request => !string.IsNullOrWhiteSpace(request.Prompt) || request.Roles.Count > 0)
			.WithMessage("Enter a team request prompt or at least one role.");
		RuleFor(request => request.Prompt)
			.MaximumLength(1000)
			.When(request => request.Prompt != null);
		RuleForEach(request => request.Roles).ChildRules(role =>
		{
			role.RuleFor(item => item.RoleTitle).NotEmpty().MaximumLength(100);
			role.RuleFor(item => item.AllocationPercent).InclusiveBetween(1, 100);
			role.RuleFor(item => item.Headcount).InclusiveBetween(1, 10);
			role.RuleFor(item => item.MinProficiency)
				.Must(level => new[] { "Beginner", "Intermediate", "Advanced" }.Contains(level, StringComparer.OrdinalIgnoreCase))
				.WithMessage("MinProficiency must be Beginner, Intermediate, or Advanced.");
		});
	}
}
