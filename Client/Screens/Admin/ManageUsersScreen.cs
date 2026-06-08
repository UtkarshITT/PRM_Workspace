using PRM.Client.Helpers;
using PRM.Client.HttpClients;
using PRM.Client.Models.Admin;

namespace PRM.Client.Screens.Admin;

public class ManageUsersScreen
{
	private readonly AdminClient _adminClient;

	public ManageUsersScreen(AdminClient adminClient)
	{
		_adminClient = adminClient;
	}

	public async Task ShowAsync()
	{
		var running = true;

		while (running)
		{
			Console.Clear();
			ConsoleHelper.WriteHeader("Manage Users");
			Console.WriteLine("  1. Create User Account");
			Console.WriteLine("  2. View All Users");
			Console.WriteLine("  3. Reset User Password");
			Console.WriteLine("  4. Deactivate User");
			Console.WriteLine("  0. Back");
			Console.WriteLine();
			Console.Write("Select option: ");

			switch (Console.ReadLine()?.Trim())
			{
				case "1":
					await CreateUserAsync();
					break;
				case "2":
					await ViewUsersAsync();
					break;
				case "3":
					await ResetPasswordAsync();
					break;
				case "4":
					await DeactivateUserAsync();
					break;
				case "0":
					running = false;
					break;
				default:
					ConsoleHelper.WriteError("Invalid option.");
					ConsoleHelper.PressEnterToContinue();
					break;
			}
		}
	}

	private async Task CreateUserAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Create User Account");

		var fullName = ConsoleHelper.ReadInput("Full Name         : ");
		var email = ConsoleHelper.ReadInput("Email             : ");
		var username = ConsoleHelper.ReadInput("Username          : ");
		var password = ConsoleHelper.ReadInput("Temporary Password: ", secret: true);
		var role = PromptRole();

		if (string.IsNullOrWhiteSpace(role))
		{
			return;
		}

		var response = await _adminClient.CreateUserAsync(new CreateUserRequest
		{
			FullName = fullName,
			Email = email,
			Username = username,
			TemporaryPassword = password,
			Role = role
		});

		if (!response.Success || response.Data == null)
		{
			WriteApiError(response);
			return;
		}

		ConsoleHelper.WriteSuccess(
			$"Account created (User ID: {response.Data.UserId}, Employee: {response.Data.EmployeeCode}). User must change password on first login.");
		ConsoleHelper.PressEnterToContinue();
	}

	private async Task ViewUsersAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("All Users");

		var response = await _adminClient.GetUsersAsync();
		if (!response.Success || response.Data == null)
		{
			WriteApiError(response);
			return;
		}

		Console.WriteLine($"{"ID",-6}{"Username",-18}{"Role",-12}{"Status",-10}Name");
		Console.WriteLine(new string('-', 70));

		foreach (var user in response.Data)
		{
			var status = user.IsActive ? "Active" : "Inactive";
			Console.WriteLine($"{user.Id,-6}{user.Username,-18}{user.Role,-12}{status,-10}{user.FullName}");
		}

		Console.WriteLine();
		Console.WriteLine($"Total: {response.Data.Count}   |   Active: {response.Data.Count(item => item.IsActive)}   |   Inactive: {response.Data.Count(item => !item.IsActive)}");
		Console.WriteLine();
		Console.Write("Reactivate user by ID (or Enter to skip): ");
		var input = Console.ReadLine()?.Trim();

		if (!string.IsNullOrWhiteSpace(input) && long.TryParse(input, out var userId))
		{
			var reactivate = await _adminClient.ReactivateUserAsync(userId);
			if (reactivate.Success)
			{
				ConsoleHelper.WriteSuccess("Account reactivated. Previous allocations are NOT restored.");
			}
			else
			{
				WriteApiError(reactivate);
			}
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private async Task ResetPasswordAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Reset User Password");

		if (!long.TryParse(ConsoleHelper.ReadInput("User ID: "), out var userId))
		{
			ConsoleHelper.WriteError("Invalid user ID.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var password = ConsoleHelper.ReadInput("New Temporary Password: ", secret: true);
		var response = await _adminClient.ResetPasswordAsync(userId, new ResetPasswordRequest
		{
			NewTemporaryPassword = password
		});

		if (response.Success)
		{
			ConsoleHelper.WriteSuccess("Password reset. User will be prompted to change it on next login.");
		}
		else
		{
			WriteApiError(response);
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private async Task DeactivateUserAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Deactivate User");

		if (!long.TryParse(ConsoleHelper.ReadInput("User ID: "), out var userId))
		{
			ConsoleHelper.WriteError("Invalid user ID.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var confirm = ConsoleHelper.ReadInput("Confirm deactivation (Y/N): ");
		if (!confirm.Equals("Y", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		var response = await _adminClient.DeactivateUserAsync(userId);
		if (response.Success)
		{
			ConsoleHelper.WriteSuccess("User account deactivated.");
		}
		else
		{
			WriteApiError(response);
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private static string? PromptRole()
	{
		Console.WriteLine("Role: (1) Admin  (2) Manager  (3) Employee");
		Console.Write("Enter choice: ");

		return Console.ReadLine()?.Trim() switch
		{
			"1" => "ADMIN",
			"2" => "MANAGER",
			"3" => "EMPLOYEE",
			_ => null
		};
	}

	private static void WriteApiError<T>(Models.ApiResponse<T> response)
	{
		ConsoleHelper.WriteError(response.Error ?? "Request failed.");

		foreach (var detail in response.Details)
		{
			ConsoleHelper.WriteError($"  - {detail}");
		}

		ConsoleHelper.PressEnterToContinue();
	}
}
