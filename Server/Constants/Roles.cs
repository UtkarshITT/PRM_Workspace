namespace PRM.Server.Constants;

public static class Roles
{
	public const string Admin = "ADMIN";
	public const string Manager = "MANAGER";
	public const string Employee = "EMPLOYEE";

	public static readonly string[] All = [Admin, Manager, Employee];
}
