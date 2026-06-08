using FluentValidation;
using PRM.Server.Models.DTOs.Employees;

namespace PRM.Server.Validators;

public class AssignManagerValidator : AbstractValidator<AssignManagerDto>
{
	public AssignManagerValidator()
	{
		RuleFor(x => x.ManagerUserId).GreaterThan(0);
	}
}
