using FluentAssertions;
using PRM.Server.Models.DTOs.Users;
using PRM.Server.Validators;

namespace PRM.Tests.Validators;

public class CreateUserValidatorTests
{
	private readonly CreateUserValidator _validator = new();

	[Fact]
	public void Validate_WithValidDto_Passes()
	{
		var result = _validator.Validate(new CreateUserDto
		{
			FullName = "Priya Sharma",
			Email = "priya@techserve.com",
			Username = "priya.sharma",
			TemporaryPassword = "Welcome1",
			Role = "EMPLOYEE"
		});

		result.IsValid.Should().BeTrue();
	}

	[Theory]
	[InlineData("short")]
	[InlineData("nouppercase1")]
	[InlineData("NoDigitsHere")]
	public void Validate_WithWeakPassword_Fails(string password)
	{
		var result = _validator.Validate(new CreateUserDto
		{
			FullName = "Test User",
			Email = "test@techserve.com",
			Username = "test.user",
			TemporaryPassword = password,
			Role = "EMPLOYEE"
		});

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_WithInvalidRole_Fails()
	{
		var result = _validator.Validate(new CreateUserDto
		{
			FullName = "Test User",
			Email = "test@techserve.com",
			Username = "test.user",
			TemporaryPassword = "Welcome1",
			Role = "DIRECTOR"
		});

		result.IsValid.Should().BeFalse();
	}
}
