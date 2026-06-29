using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PRM.Server.Constants;
using PRM.Server.Controllers;
using PRM.Server.Models.DTOs.Ai;
using PRM.Server.Models.DTOs.Allocations;
using PRM.Server.Models.DTOs.Employees;
using PRM.Server.Models.DTOs.Projects;
using PRM.Server.Models.DTOs.Timesheets;
using PRM.Server.Models.DTOs.Users;
using PRM.Server.Services.Interfaces;

namespace PRM.Tests.Services;

public class RbacAuthorizationTests : IAsyncLifetime
{
	private IHost _host = null!;
	private HttpClient _client = null!;

	[Fact]
	public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
	{
		var response = await _client.GetAsync("/api/users");

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task AdminEndpoint_WithEmployeeRole_ReturnsForbidden()
	{
		using var request = CreateRequest(HttpMethod.Get, "/api/users", Roles.Employee);

		var response = await _client.SendAsync(request);

		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task AdminEndpoint_WithAdminRole_ReturnsOk()
	{
		using var request = CreateRequest(HttpMethod.Get, "/api/users", Roles.Admin);

		var response = await _client.SendAsync(request);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task ManagerEndpoint_WithManagerRole_ReturnsOk()
	{
		using var request = CreateRequest(HttpMethod.Get, "/api/projects/my", Roles.Manager);

		var response = await _client.SendAsync(request);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task ManagerEndpoint_WithEmployeeRole_ReturnsForbidden()
	{
		using var request = CreateRequest(HttpMethod.Get, "/api/ai/skill-match?req=dotnet", Roles.Employee);

		var response = await _client.SendAsync(request);

		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task EmployeeEndpoint_WithEmployeeRoleAndResourceProfileClaim_ReturnsOk()
	{
		using var request = CreateRequest(HttpMethod.Get, "/api/allocations/my", Roles.Employee, resourceProfileId: 10);

		var response = await _client.SendAsync(request);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task EmployeeEndpoint_WithManagerRole_ReturnsForbidden()
	{
		using var request = CreateRequest(HttpMethod.Get, "/api/allocations/my", Roles.Manager);

		var response = await _client.SendAsync(request);

		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task HealthEndpoint_WithoutToken_ReturnsOk()
	{
		var response = await _client.GetAsync("/health");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task AuditLogsEndpoint_WithAdminRole_ReturnsOk()
	{
		using var request = CreateRequest(HttpMethod.Get, "/api/audit-logs", Roles.Admin);

		var response = await _client.SendAsync(request);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task AuditLogsEndpoint_WithManagerRole_ReturnsForbidden()
	{
		using var request = CreateRequest(HttpMethod.Get, "/api/audit-logs", Roles.Manager);

		var response = await _client.SendAsync(request);

		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	public async Task InitializeAsync()
	{
		_host = await new HostBuilder()
			.ConfigureWebHost(webBuilder =>
			{
				webBuilder.UseTestServer();
				webBuilder.ConfigureServices(ConfigureServices);
				webBuilder.Configure(app =>
				{
					app.UseRouting();
					app.UseAuthentication();
					app.UseAuthorization();
					app.UseEndpoints(endpoints =>
					{
						endpoints.MapControllers();
						endpoints.MapGet("/health", () => Results.Ok()).AllowAnonymous();
					});
				});
			})
			.StartAsync();

		_client = _host.GetTestClient();
	}

	public async Task DisposeAsync()
	{
		_client.Dispose();
		_host.Dispose();
		await Task.CompletedTask;
	}

	private static void ConfigureServices(IServiceCollection services)
	{
		services.AddAuthentication(TestAuthHandler.SchemeName)
			.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
		services.AddAuthorization(options =>
		{
			options.AddPolicy(AuthorizationPolicies.AdminOnly, policy => policy.RequireRole(Roles.Admin));
			options.AddPolicy(AuthorizationPolicies.ManagerOnly, policy => policy.RequireRole(Roles.Manager));
			options.AddPolicy(AuthorizationPolicies.EmployeeOnly, policy => policy.RequireRole(Roles.Employee));
			options.FallbackPolicy = new AuthorizationPolicyBuilder()
				.RequireAuthenticatedUser()
				.Build();
		});
		services.AddControllers().AddApplicationPart(typeof(UserController).Assembly);

		RegisterValidators(services);
		RegisterServiceMocks(services);
	}

	private static void RegisterValidators(IServiceCollection services)
	{
		services.AddSingleton<IValidator<CreateUserDto>, InlineValidator<CreateUserDto>>();
		services.AddSingleton<IValidator<ResetPasswordDto>, InlineValidator<ResetPasswordDto>>();
		services.AddSingleton<IValidator<UpdateUserRoleDto>, InlineValidator<UpdateUserRoleDto>>();
		services.AddSingleton<IValidator<UpdateEmployeeDto>, InlineValidator<UpdateEmployeeDto>>();
		services.AddSingleton<IValidator<AddSkillDto>, InlineValidator<AddSkillDto>>();
		services.AddSingleton<IValidator<AssignManagerDto>, InlineValidator<AssignManagerDto>>();
		services.AddSingleton<IValidator<CreateProjectDto>, InlineValidator<CreateProjectDto>>();
		services.AddSingleton<IValidator<UpdateProjectDto>, InlineValidator<UpdateProjectDto>>();
		services.AddSingleton<IValidator<CreateMilestoneDto>, InlineValidator<CreateMilestoneDto>>();
		services.AddSingleton<IValidator<UpdateMilestoneStatusDto>, InlineValidator<UpdateMilestoneStatusDto>>();
		services.AddSingleton<IValidator<CreateAllocationDto>, InlineValidator<CreateAllocationDto>>();
		services.AddSingleton<IValidator<SubmitTimesheetDto>, InlineValidator<SubmitTimesheetDto>>();
		services.AddSingleton<IValidator<TeamBuilderRequestDto>, InlineValidator<TeamBuilderRequestDto>>();
	}

	private static void RegisterServiceMocks(IServiceCollection services)
	{
		var userService = new Mock<IUserService>();
		userService
			.Setup(service => service.GetAllUsersAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var projectService = new Mock<IProjectService>();
		projectService
			.Setup(service => service.GetMyProjectsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var allocationService = new Mock<IAllocationService>();
		allocationService
			.Setup(service => service.GetMyAllocationsAsync(It.IsAny<long>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new EmployeeAllocationsResponseDto());

		var aiService = new Mock<IAiIntegrationService>();
		aiService
			.Setup(service => service.GetSkillMatchAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new AiSkillMatchResponseDto());

		services.AddSingleton(userService.Object);
		services.AddSingleton(projectService.Object);
		services.AddSingleton(allocationService.Object);
		services.AddSingleton(aiService.Object);
		services.AddSingleton(Mock.Of<IAuthService>());
		services.AddSingleton(Mock.Of<IResourceProfileService>());
		services.AddSingleton(Mock.Of<ITimesheetService>());
		services.AddSingleton(Mock.Of<ISystemConfigService>());
		services.AddSingleton(Mock.Of<IAuditService>());
		services.AddSingleton(Mock.Of<INotificationLogService>());
	}

	private static HttpRequestMessage CreateRequest(
		HttpMethod method,
		string path,
		string role,
		long userId = 1,
		long? resourceProfileId = null)
	{
		var request = new HttpRequestMessage(method, path);
		request.Headers.Add(TestAuthHandler.RoleHeader, role);
		request.Headers.Add(TestAuthHandler.UserIdHeader, userId.ToString());

		if (resourceProfileId.HasValue)
		{
			request.Headers.Add(TestAuthHandler.ResourceProfileIdHeader, resourceProfileId.Value.ToString());
		}

		return request;
	}

	private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
	{
		public const string SchemeName = "Test";
		public const string RoleHeader = "X-Test-Role";
		public const string UserIdHeader = "X-Test-User-Id";
		public const string ResourceProfileIdHeader = "X-Test-Resource-Profile-Id";

		public TestAuthHandler(
			IOptionsMonitor<AuthenticationSchemeOptions> options,
			ILoggerFactory logger,
			UrlEncoder encoder)
			: base(options, logger, encoder)
		{
		}

		protected override Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			if (!Request.Headers.TryGetValue(RoleHeader, out var role))
			{
				return Task.FromResult(AuthenticateResult.NoResult());
			}

			var userId = Request.Headers.TryGetValue(UserIdHeader, out var userIdValue)
				? userIdValue.ToString()
				: "1";
			var claims = new List<Claim>
			{
				new(ClaimTypes.NameIdentifier, userId),
				new(ClaimTypes.Role, role.ToString())
			};

			if (Request.Headers.TryGetValue(ResourceProfileIdHeader, out var resourceProfileId))
			{
				claims.Add(new Claim("resource_profile_id", resourceProfileId.ToString()));
			}

			var identity = new ClaimsIdentity(claims, SchemeName);
			var principal = new ClaimsPrincipal(identity);
			var ticket = new AuthenticationTicket(principal, SchemeName);

			return Task.FromResult(AuthenticateResult.Success(ticket));
		}
	}
}
