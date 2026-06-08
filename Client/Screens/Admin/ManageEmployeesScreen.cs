using PRM.Client.Helpers;
using PRM.Client.HttpClients;
using PRM.Client.Models.Admin;

namespace PRM.Client.Screens.Admin;

public class ManageEmployeesScreen
{
	private readonly AdminClient _adminClient;

	public ManageEmployeesScreen(AdminClient adminClient)
	{
		_adminClient = adminClient;
	}

	public async Task ShowAsync()
	{
		var running = true;

		while (running)
		{
			Console.Clear();
			ConsoleHelper.WriteHeader("Manage Employees");
			Console.WriteLine("  1. View All Employees");
			Console.WriteLine("  2. Update Employee");
			Console.WriteLine("  3. Deactivate Employee");
			Console.WriteLine("  4. Manage Employee Skills");
			Console.WriteLine("  5. Assign Manager");
			Console.WriteLine("  0. Back");
			Console.WriteLine();
			Console.Write("Select option: ");

			switch (Console.ReadLine()?.Trim())
			{
				case "1":
					await ViewEmployeesAsync();
					break;
				case "2":
					await UpdateEmployeeAsync();
					break;
				case "3":
					await DeactivateEmployeeAsync();
					break;
				case "4":
					await ManageSkillsAsync();
					break;
				case "5":
					await AssignManagerAsync();
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

	private async Task ViewEmployeesAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("All Employees");

		Console.Write("Filter by status (BENCH/ALLOCATED or Enter): ");
		var status = Console.ReadLine()?.Trim();
		Console.Write("Filter by department (or Enter): ");
		var department = Console.ReadLine()?.Trim();

		var response = await _adminClient.GetEmployeesAsync(
			string.IsNullOrWhiteSpace(status) ? null : status,
			string.IsNullOrWhiteSpace(department) ? null : department);

		if (!response.Success || response.Data == null)
		{
			WriteApiError(response);
			return;
		}

		Console.WriteLine($"{"ID",-6}{"Name",-20}{"Department",-14}{"Status",-12}Skills");
		Console.WriteLine(new string('-', 80));

		foreach (var employee in response.Data)
		{
			var skills = employee.Skills.Count == 0 ? "-" : string.Join(", ", employee.Skills);
			Console.WriteLine(
				$"{employee.Id,-6}{employee.FullName,-20}{employee.Department ?? "-",-14}{employee.EmploymentStatus,-12}{skills}");
		}

		Console.WriteLine();
		Console.WriteLine(
			$"Total: {response.Data.Count}   |   Allocated: {response.Data.Count(item => item.EmploymentStatus == "ALLOCATED")}   |   Bench: {response.Data.Count(item => item.EmploymentStatus == "BENCH")}");
		ConsoleHelper.PressEnterToContinue();
	}

	private async Task UpdateEmployeeAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Update Employee");

		if (!long.TryParse(ConsoleHelper.ReadInput("Employee ID: "), out var employeeId))
		{
			ConsoleHelper.WriteError("Invalid employee ID.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var fullName = ConsoleHelper.ReadInput("Full Name   : ");
		var department = ConsoleHelper.ReadInput("Department  : ");
		var designation = ConsoleHelper.ReadInput("Designation : ");

		var response = await _adminClient.UpdateEmployeeAsync(employeeId, new UpdateEmployeeRequest
		{
			FullName = fullName,
			Department = string.IsNullOrWhiteSpace(department) ? null : department,
			Designation = string.IsNullOrWhiteSpace(designation) ? null : designation
		});

		if (response.Success)
		{
			ConsoleHelper.WriteSuccess("Employee updated.");
		}
		else
		{
			WriteApiError(response);
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private async Task DeactivateEmployeeAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Deactivate Employee");

		if (!long.TryParse(ConsoleHelper.ReadInput("Employee ID: "), out var employeeId))
		{
			ConsoleHelper.WriteError("Invalid employee ID.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var confirm = ConsoleHelper.ReadInput("Confirm deactivation (Y/N): ");
		if (!confirm.Equals("Y", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		var response = await _adminClient.DeactivateEmployeeAsync(employeeId);
		if (response.Success)
		{
			ConsoleHelper.WriteSuccess("Employee deactivated. Active allocations ended and login blocked.");
		}
		else
		{
			WriteApiError(response);
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private async Task ManageSkillsAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Manage Employee Skills");

		if (!long.TryParse(ConsoleHelper.ReadInput("Employee ID: "), out var employeeId))
		{
			ConsoleHelper.WriteError("Invalid employee ID.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var running = true;
		while (running)
		{
			Console.Clear();
			ConsoleHelper.WriteHeader($"Skills — Employee {employeeId}");
			Console.WriteLine("  1. Add Skill");
			Console.WriteLine("  2. Remove Skill");
			Console.WriteLine("  0. Back");
			Console.Write("Select option: ");

			switch (Console.ReadLine()?.Trim())
			{
				case "1":
					await AddSkillAsync(employeeId);
					break;
				case "2":
					await RemoveSkillAsync(employeeId);
					break;
				case "0":
					running = false;
					break;
			}
		}
	}

	private async Task AddSkillAsync(long employeeId)
	{
		var skillName = ConsoleHelper.ReadInput("Skill Name: ");
		var category = PromptCategory();
		var proficiency = PromptProficiency();

		if (category == null || proficiency == null)
		{
			return;
		}

		var response = await _adminClient.AddSkillAsync(employeeId, new AddSkillRequest
		{
			SkillName = skillName,
			Category = category,
			ProficiencyLevel = proficiency
		});

		if (response.Success)
		{
			ConsoleHelper.WriteSuccess("Skill added.");
		}
		else
		{
			WriteApiError(response);
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private async Task RemoveSkillAsync(long employeeId)
	{
		if (!long.TryParse(ConsoleHelper.ReadInput("Skill ID to remove: "), out var skillId))
		{
			ConsoleHelper.WriteError("Invalid skill ID.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var response = await _adminClient.RemoveSkillAsync(employeeId, skillId);
		if (response.Success)
		{
			ConsoleHelper.WriteSuccess("Skill removed.");
		}
		else
		{
			WriteApiError(response);
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private async Task AssignManagerAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Assign Manager");

		if (!long.TryParse(ConsoleHelper.ReadInput("Employee ID: "), out var employeeId))
		{
			ConsoleHelper.WriteError("Invalid employee ID.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		if (!long.TryParse(ConsoleHelper.ReadInput("Manager User ID: "), out var managerUserId))
		{
			ConsoleHelper.WriteError("Invalid manager user ID.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var response = await _adminClient.AssignManagerAsync(employeeId, new AssignManagerRequest
		{
			ManagerUserId = managerUserId
		});

		if (response.Success)
		{
			ConsoleHelper.WriteSuccess("Manager assigned.");
		}
		else
		{
			WriteApiError(response);
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private static string? PromptCategory()
	{
		Console.WriteLine("Category: (1) Backend  (2) Frontend  (3) DevOps  (4) QA  (5) Other");
		Console.Write("Enter choice: ");

		return Console.ReadLine()?.Trim() switch
		{
			"1" => "Backend",
			"2" => "Frontend",
			"3" => "DevOps",
			"4" => "QA",
			"5" => "Other",
			_ => null
		};
	}

	private static string? PromptProficiency()
	{
		Console.WriteLine("Proficiency: (1) Beginner  (2) Intermediate  (3) Advanced");
		Console.Write("Enter choice: ");

		return Console.ReadLine()?.Trim() switch
		{
			"1" => "Beginner",
			"2" => "Intermediate",
			"3" => "Advanced",
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
