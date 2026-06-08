using FluentValidation;
using PRM.Server.Constants;
using PRM.Server.Models.DTOs.Users;

namespace PRM.Server.Validators;

public class CreateUserValidator : AbstractValidator<CreateUserDto>
{
	public CreateUserValidator()
	{
		RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
		RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
		RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
		RuleFor(x => x.TemporaryPassword)
			.NotEmpty()
			.MinimumLength(8)
			.Matches("[A-Z]").WithMessage("Temporary password must contain at least one uppercase letter.")
			.Matches("[0-9]").WithMessage("Temporary password must contain at least one number.");
		RuleFor(x => x.Role)
			.NotEmpty()
			.Must(role => Roles.All.Contains(role))
			.WithMessage($"Role must be one of: {string.Join(", ", Roles.All)}.");
	}
}
