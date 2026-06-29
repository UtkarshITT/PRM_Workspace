using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using PRM.Server.Configuration;
using PRM.Server.Constants;
using PRM.Server.Data;
using PRM.Server.Middleware;
using PRM.Server.Repositories;
using PRM.Server.Repositories.Interfaces;
using PRM.Server.Scheduler;
using PRM.Server.Seed;
using PRM.Server.Services;
using PRM.Server.Services.Ai;
using PRM.Server.Services.Email;
using PRM.Server.Services.Interfaces;
using PRM.Server.Validators;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
	.ReadFrom.Configuration(builder.Configuration)
	.Enrich.FromLogContext()
	.WriteTo.Console()
	.CreateLogger();

builder.Host.UseSerilog();

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
	?? throw new InvalidOperationException("JwtSettings configuration is missing.");

var jwtSecret = Environment.GetEnvironmentVariable("PRM_JWT_SECRET");
if (!string.IsNullOrWhiteSpace(jwtSecret))
{
	jwtSettings.SecretKey = jwtSecret;
}

if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey) || jwtSettings.SecretKey.Length < 32)
{
	throw new InvalidOperationException("JWT secret must be at least 32 characters. Set JwtSettings:SecretKey or PRM_JWT_SECRET.");
}

builder.Services.Configure<JwtSettings>(options =>
{
	options.SecretKey = jwtSettings.SecretKey;
	options.Issuer = jwtSettings.Issuer;
	options.Audience = jwtSettings.Audience;
	options.ExpiryHours = jwtSettings.ExpiryHours;
});

builder.Services.AddDbContext<PrmDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDataProtection();
builder.Services.AddHttpContextAccessor();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IResourceProfileRepository, ResourceProfileRepository>();
builder.Services.AddScoped<ISkillRepository, SkillRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IAllocationRepository, AllocationRepository>();
builder.Services.AddScoped<ITimesheetRepository, TimesheetRepository>();
builder.Services.AddScoped<IActivityTagRepository, ActivityTagRepository>();
builder.Services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();
builder.Services.AddScoped<ISchedulerJobLogRepository, SchedulerJobLogRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IAiRequestLogRepository, AiRequestLogRepository>();
builder.Services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
builder.Services.AddScoped<IEmailNotificationService, EmailNotificationService>();
builder.Services.AddHttpClient("GeminiLlm", client =>
{
	client.BaseAddress = new Uri(builder.Configuration["Llm:Gemini:BaseUrl"] ?? "https://generativelanguage.googleapis.com/");
	client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("GroqLlm", client =>
{
	client.BaseAddress = new Uri(builder.Configuration["Llm:Groq:BaseUrl"] ?? "https://api.groq.com/");
	client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("CustomGenerateLlm", client =>
{
	client.BaseAddress = new Uri(builder.Configuration["Llm:Custom:BaseUrl"] ?? "http://localhost:11434");
	client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<ILlmClientRegistration, GeminiLlmClientRegistration>();
builder.Services.AddScoped<ILlmClientRegistration, GroqLlmClientRegistration>();
builder.Services.AddScoped<ILlmClientRegistration, CustomLlmClientRegistration>();
builder.Services.AddScoped<ILlmClientFactory, LlmClientFactory>();
builder.Services.AddScoped<IAiIntegrationService, AiIntegrationService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IResourceProfileService, ResourceProfileService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IAllocationService, AllocationService>();
builder.Services.AddScoped<ITimesheetService, TimesheetService>();
builder.Services.AddScoped<ISystemConfigService, SystemConfigService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IComplianceNotificationService, ComplianceNotificationService>();
builder.Services.AddScoped<INotificationLogService, NotificationLogService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddHostedService<BackgroundScheduler>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = jwtSettings.Issuer,
			ValidAudience = jwtSettings.Audience,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
		};
	});

builder.Services.AddAuthorization(options =>
{
	options.AddPolicy(AuthorizationPolicies.AdminOnly, policy => policy.RequireRole(Roles.Admin));
	options.AddPolicy(AuthorizationPolicies.ManagerOnly, policy => policy.RequireRole(Roles.Manager));
	options.AddPolicy(AuthorizationPolicies.EmployeeOnly, policy => policy.RequireRole(Roles.Employee));
	options.FallbackPolicy = new AuthorizationPolicyBuilder()
		.RequireAuthenticatedUser()
		.Build();
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (builder.Environment.IsDevelopment())
{
	builder.WebHost.UseUrls("https://localhost:5001", "http://localhost:5000");
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var context = scope.ServiceProvider.GetRequiredService<PrmDbContext>();
	await DatabaseSeeder.SeedAsync(context);
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<ForcePasswordChangeMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
	status = "Healthy",
	timestamp = DateTime.UtcNow
})).AllowAnonymous().WithTags("Health");

app.Run();

public partial class Program
{
}
