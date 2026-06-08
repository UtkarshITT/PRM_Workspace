using FluentValidation;

namespace PRM.Server.Helpers;

public static class ValidationHelper
{
	public static async Task ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken = default)
	{
		var result = await validator.ValidateAsync(instance, cancellationToken);

		if (!result.IsValid)
		{
			throw new Exceptions.ValidationException(
				"Validation failed.",
				result.Errors.Select(error => error.ErrorMessage));
		}
	}
}
