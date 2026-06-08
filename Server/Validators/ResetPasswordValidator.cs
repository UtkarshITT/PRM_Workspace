using FluentValidation;
using PRM.Server.Models.DTOs.Users;

namespace PRM.Server.Validators;

public class ResetPasswordValidator : AbstractValidator<ResetPasswordDto>
{
	public ResetPasswordValidator()
	{
		RuleFor(x => x.NewTemporaryPassword)
			.NotEmpty()
			.MinimumLength(8)
			.Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
			.Matches("[0-9]").WithMessage("Password must contain at least one number.");
	}
}
