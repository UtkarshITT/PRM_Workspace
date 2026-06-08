using FluentValidation;
using PRM.Server.Models.DTOs.Employees;

namespace PRM.Server.Validators;

public class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeDto>
{
	public UpdateEmployeeValidator()
	{
		RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
		RuleFor(x => x.Department).MaximumLength(100);
		RuleFor(x => x.Designation).MaximumLength(100);
	}
}
