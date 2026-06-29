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
			Console.WriteLine("  4. Manage User Roles");
			Console.WriteLine("  5. View Role Permissions");
			Console.WriteLine("  6. Deactivate User");
			Console.WriteLine("  7. Back");
			Console.WriteLine();
			Console.Write("Enter option: ");

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
					await ChangeUserRoleAsync();
					break;
				case "5":
					await ViewRolePermissionsAsync();
					break;
				case "6":
					await DeactivateUserAsync();
					break;
				case "7":
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

		if (!TryReadRequired("Full Name         : ", "Full name is required.", out var fullName))
		{
			return;
		}

		if (!TryReadRequired("Email             : ", "Email is required.", out var email))
		{
			return;
		}

		if (!email.Contains('@', StringComparison.Ordinal))
		{
			ConsoleHelper.WriteError("Email must be valid.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		if (!TryReadRequired("Username          : ", "Username is required.", out var username))
		{
			return;
		}

		var password = ConsoleHelper.ReadInput("Temporary Password: ", secret: true);
		if (password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsDigit))
		{
			ConsoleHelper.WriteError("Temporary password must be at least 8 characters and include one uppercase letter and one number.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

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
				return;
			}
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private async Task ResetPasswordAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Reset User Password");

		if (!TryReadPositiveLong("User ID: ", "User ID must be a positive number.", out var userId))
		{
			return;
		}

		var password = ConsoleHelper.ReadInput("New Temporary Password: ", secret: true);
		if (password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsDigit))
		{
			ConsoleHelper.WriteError("Temporary password must be at least 8 characters and include one uppercase letter and one number.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

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
			return;
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private async Task ChangeUserRoleAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Manage User Roles");

		if (!TryReadPositiveLong("User ID: ", "User ID must be a positive number.", out var userId))
		{
			return;
		}

		var user = await GetUserForUpdateAsync(userId);
		if (user == null)
		{
			return;
		}

		ConsoleHelper.WriteKeepCurrentHint();
		var role = PromptRole(user.Role);
		if (string.IsNullOrWhiteSpace(role))
		{
			ConsoleHelper.WriteError("Invalid role selection.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var response = await _adminClient.UpdateUserRoleAsync(userId, new UpdateUserRoleRequest
		{
			Role = role
		});

		if (response.Success)
		{
			ConsoleHelper.WriteSuccess("User role updated. The user must log in again for the new permissions.");
		}
		else
		{
			WriteApiError(response);
			return;
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private async Task ViewRolePermissionsAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Role Permissions");

		var response = await _adminClient.GetRolePermissionsAsync();
		if (!response.Success || response.Data == null)
		{
			WriteApiError(response);
			return;
		}

		foreach (var role in response.Data)
		{
			Console.WriteLine(role.Role);
			Console.WriteLine(new string('-', role.Role.Length));
			foreach (var permission in role.Permissions)
			{
				Console.WriteLine($"  - {permission}");
			}

			Console.WriteLine();
		}

		ConsoleHelper.PressEnterToContinue();
	}


	private async Task<UserListItem?> GetUserForUpdateAsync(long userId)
	{
		var response = await _adminClient.GetUsersAsync();
		if (!response.Success || response.Data == null)
		{
			WriteApiError(response);
			return null;
		}

		var user = response.Data.FirstOrDefault(item => item.Id == userId);
		if (user != null)
		{
			return user;
		}

		ConsoleHelper.WriteError("User not found.");
		ConsoleHelper.PressEnterToContinue();
		return null;
	}

	private async Task DeactivateUserAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Deactivate User");

		if (!TryReadPositiveLong("User ID: ", "User ID must be a positive number.", out var userId))
		{
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
			return;
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private static string? PromptRole(string? currentRole = null)
	{
		Console.WriteLine("Role: (1) Admin  (2) Manager  (3) Employee");
		Console.Write(string.IsNullOrWhiteSpace(currentRole) ? "Enter choice: " : $"Enter choice [{currentRole}]: ");

		var input = Console.ReadLine()?.Trim();
		if (string.IsNullOrWhiteSpace(input) && !string.IsNullOrWhiteSpace(currentRole))
		{
			return currentRole;
		}

		return input switch
		{
			"1" => "ADMIN",
			"2" => "MANAGER",
			"3" => "EMPLOYEE",
			_ => null
		};
	}

	private static bool TryReadPositiveLong(string prompt, string error, out long value)
	{
		if (!long.TryParse(ConsoleHelper.ReadInput(prompt), out value) || value <= 0)
		{
			ConsoleHelper.WriteError(error);
			ConsoleHelper.PressEnterToContinue();
			return false;
		}

		return true;
	}

	private static bool TryReadRequired(string prompt, string error, out string value)
	{
		value = ConsoleHelper.ReadInput(prompt);
		if (!string.IsNullOrWhiteSpace(value))
		{
			return true;
		}

		ConsoleHelper.WriteError(error);
		ConsoleHelper.PressEnterToContinue();
		return false;
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
