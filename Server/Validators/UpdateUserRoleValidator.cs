using FluentValidation;
using PRM.Server.Constants;
using PRM.Server.Models.DTOs.Users;

namespace PRM.Server.Validators;

public class UpdateUserRoleValidator : AbstractValidator<UpdateUserRoleDto>
{
	public UpdateUserRoleValidator()
	{
		RuleFor(x => x.Role)
			.NotEmpty()
			.Must(role => Roles.All.Contains(role))
			.WithMessage($"Role must be one of: {string.Join(", ", Roles.All)}.");
	}
}
