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
			Console.WriteLine("  6. Back");
			Console.WriteLine();
			Console.Write("Enter option: ");

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
				case "6":
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

		var employee = await GetEmployeeForUpdateAsync(employeeId);
		if (employee == null)
		{
			return;
		}

		ConsoleHelper.WriteKeepCurrentHint();
		var fullNameInput = ConsoleHelper.ReadOptionalUpdateInput("Full Name  ", employee.FullName);
		var departmentInput = ConsoleHelper.ReadOptionalUpdateInput("Department ", employee.Department);
		var designationInput = ConsoleHelper.ReadOptionalUpdateInput("Designation", employee.Designation);

		var fullName = string.IsNullOrWhiteSpace(fullNameInput) ? employee.FullName : fullNameInput;
		var department = ResolveOptionalUpdateValue(departmentInput, employee.Department);
		var designation = ResolveOptionalUpdateValue(designationInput, employee.Designation);

		var response = await _adminClient.UpdateEmployeeAsync(employeeId, new UpdateEmployeeRequest
		{
			FullName = fullName,
			Department = department,
			Designation = designation
		});

		if (response.Success)
		{
			ConsoleHelper.WriteSuccess("Employee updated.");
		}
		else
		{
			WriteApiError(response);
			return;
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private async Task<EmployeeListItem?> GetEmployeeForUpdateAsync(long employeeId)
	{
		var response = await _adminClient.GetEmployeesAsync();
		if (!response.Success || response.Data == null)
		{
			WriteApiError(response);
			return null;
		}

		var employee = response.Data.FirstOrDefault(item => item.Id == employeeId);
		if (employee != null)
		{
			return employee;
		}

		ConsoleHelper.WriteError("Employee not found.");
		ConsoleHelper.PressEnterToContinue();
		return null;
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
			return;
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

			if (!await DisplayEmployeeSkillsAsync(employeeId))
			{
				return;
			}

			Console.WriteLine("  1. Add Skill");
			Console.WriteLine("  2. Update Proficiency Level");
			Console.WriteLine("  3. Remove Skill");
			Console.WriteLine("  4. Back");
			Console.Write("Enter option: ");

			switch (Console.ReadLine()?.Trim())
			{
				case "1":
					await AddSkillAsync(employeeId);
					break;
				case "2":
					await UpdateSkillProficiencyAsync(employeeId);
					break;
				case "3":
					await RemoveSkillAsync(employeeId);
					break;
				case "4":
					running = false;
					break;
			}
		}
	}

	private async Task<bool> DisplayEmployeeSkillsAsync(long employeeId)
	{
		var response = await _adminClient.GetEmployeesAsync();
		if (!response.Success)
		{
			WriteApiError(response);
			return false;
		}

		var employee = response.Data?.FirstOrDefault(item => item.Id == employeeId);
		if (employee == null)
		{
			ConsoleHelper.WriteError($"Employee with ID {employeeId} was not found.");
			ConsoleHelper.PressEnterToContinue();
			return false;
		}

		Console.WriteLine($"{employee.FullName} | {employee.Department ?? "-"} | {employee.EmploymentStatus}");
		Console.WriteLine();

		if (employee.SkillDetails.Count == 0)
		{
			Console.WriteLine("No skills assigned yet.");
			Console.WriteLine();
			return true;
		}

		Console.WriteLine($"{"Skill ID",-10}{"Skill",-24}{"Category",-14}{"Proficiency"}");
		Console.WriteLine(new string('-', 62));

		foreach (var skill in employee.SkillDetails.OrderBy(item => item.SkillId))
		{
			Console.WriteLine($"{skill.SkillId,-10}{skill.SkillName,-24}{skill.Category,-14}{skill.ProficiencyLevel}");
		}

		Console.WriteLine();
		return true;
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
			return;
		}

		ConsoleHelper.PressEnterToContinue();
	}

	private async Task UpdateSkillProficiencyAsync(long employeeId)
	{
		if (!long.TryParse(ConsoleHelper.ReadInput("Skill ID to update: "), out var skillId))
		{
			ConsoleHelper.WriteError("Invalid skill ID.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var proficiency = PromptProficiency();
		if (proficiency == null)
		{
			ConsoleHelper.WriteError("Invalid proficiency level.");
			ConsoleHelper.PressEnterToContinue();
			return;
		}

		var response = await _adminClient.UpdateSkillProficiencyAsync(
			employeeId,
			skillId,
			new UpdateSkillProficiencyRequest
			{
				ProficiencyLevel = proficiency
			});

		if (response.Success)
		{
			ConsoleHelper.WriteSuccess("Skill proficiency updated.");
		}
		else
		{
			WriteApiError(response);
			return;
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
			return;
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
			return;
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

	private static string? ResolveOptionalUpdateValue(string input, string? currentValue)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			return currentValue;
		}

		return input == "-" ? null : input;
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
