using Microsoft.Extensions.Configuration;
using PRM.Client;
using PRM.Client.Helpers;
using PRM.Client.HttpClients;
using PRM.Client.Screens;
using PRM.Client.Screens.Admin;
using PRM.Client.Screens.Employee;
using PRM.Client.Screens.Manager;

var configuration = new ConfigurationBuilder()
	.SetBasePath(AppContext.BaseDirectory)
	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
	.Build();

var serverBaseUrl = configuration["ServerBaseUrl"] ?? "https://localhost:5001";
var restClient = new RestClient(serverBaseUrl);
var authClient = new AuthClient(restClient);
var adminClient = new AdminClient(restClient);
var managerClient = new ManagerClient(restClient);

var app = new AppStarter(
	serverBaseUrl,
	new LoginScreen(authClient),
	new ChangePasswordScreen(authClient),
	new AdminMenuScreen(
		new ManageUsersScreen(adminClient),
		new ManageEmployeesScreen(adminClient),
		new ManageProjectsScreen(adminClient),
		new ViewAllocationsScreen(adminClient)),
	new ManagerMenuScreen(
		new ResourceDashboardScreen(managerClient),
		new AllocateResourceScreen(managerClient)),
	new EmployeeMenuScreen());

await app.RunAsync();

Console.WriteLine("Goodbye.");
