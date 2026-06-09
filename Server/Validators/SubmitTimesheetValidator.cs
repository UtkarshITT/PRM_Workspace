using FluentValidation;
using PRM.Server.Models.DTOs.Timesheets;

namespace PRM.Server.Validators;

public class SubmitTimesheetValidator : AbstractValidator<SubmitTimesheetDto>
{
	public SubmitTimesheetValidator()
	{
		RuleFor(x => x.WeekStartDate).NotEmpty();
		RuleFor(x => x.LineItems).NotEmpty();
		RuleForEach(x => x.LineItems).ChildRules(lineItem =>
		{
			lineItem.RuleFor(x => x.ProjectId).GreaterThan(0);
			lineItem.RuleFor(x => x.HoursLogged).GreaterThan(0).LessThanOrEqualTo(168);
			lineItem.RuleFor(x => x.ActivityTagIds).NotEmpty();
		});
		RuleFor(x => x.Remarks).MaximumLength(500).When(x => x.Remarks != null);
	}
}
