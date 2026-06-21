using PRM.Client.Helpers;
using PRM.Client.HttpClients;
using PRM.Client.Models.Admin;

namespace PRM.Client.Screens.Admin;

public class SystemConfigScreen
{
	private readonly AdminClient _adminClient;

	public SystemConfigScreen(AdminClient adminClient)
	{
		_adminClient = adminClient;
	}

	public async Task ShowAsync()
	{
		var running = true;

		while (running)
		{
			Console.Clear();
			ConsoleHelper.WriteHeader("System Configuration");
			Console.WriteLine("  1. View Current Settings");
			Console.WriteLine("  2. Update Settings");
			Console.WriteLine("  3. Back");
			Console.WriteLine();
			Console.Write("Enter option: ");

			switch (Console.ReadLine()?.Trim())
			{
				case "1":
					await ViewConfigAsync();
					break;
				case "2":
					await UpdateConfigAsync();
					break;
				case "3":
					running = false;
					break;
				default:
					ConsoleHelper.WriteError("Invalid option.");
					ConsoleHelper.PressEnterToContinue();
					break;
			}
		}
	}

	private async Task ViewConfigAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Current System Configuration");

		var response = await _adminClient.GetSystemConfigAsync();
		if (!response.Success || response.Data == null)
		{
			WriteApiError(response);
			return;
		}

		WriteConfigTable(response.Data);
		ConsoleHelper.PressEnterToContinue();
	}

	private async Task UpdateConfigAsync()
	{
		Console.Clear();
		ConsoleHelper.WriteHeader("Update System Configuration");

		var currentResponse = await _adminClient.GetSystemConfigAsync();
		if (!currentResponse.Success || currentResponse.Data == null)
		{
			WriteApiError(currentResponse);
			return;
		}

		var configs = currentResponse.Data;
		Console.WriteLine("Press Enter to keep the current value. Enter blank LLM API key to keep it unchanged.");
		Console.WriteLine();

		var request = new UpdateSystemConfigRequest();
		var provider = ConsoleHelper.ReadOptionalUpdateInput(
			"LLM Provider (Gemini/Groq/Custom)",
			GetConfigValue(configs, "llm_provider"));
		if (!string.IsNullOrWhiteSpace(provider))
		{
			request.LlmProvider = provider;
		}

		var apiKeyConfig = configs.FirstOrDefault(config => config.Key.Equals("llm_api_key", StringComparison.OrdinalIgnoreCase));
		var apiKeyDisplay = apiKeyConfig?.IsConfigured == true ? "****" : "not configured";
		var apiKey = ConsoleHelper.ReadOptionalUpdateInput("LLM API Key (secret)     ", apiKeyDisplay, secret: true);
		if (!string.IsNullOrWhiteSpace(apiKey))
		{
			request.LlmApiKey = apiKey;
		}

		if (!TryReadOptionalInt(
			    "Scheduler Interval Hours ",
			    GetConfigValue(configs, "scheduler_interval_hours"),
			    out var schedulerIntervalHours)
		    || !TryReadOptionalInt(
			    "Max Weekly Hours         ",
			    GetConfigValue(configs, "max_weekly_hours"),
			    out var maxWeeklyHours)
		    || !TryReadOptionalBool(
			    "Console Email Enabled (Y/N)",
			    GetConfigValue(configs, "email_console_enabled"),
			    out var emailConsoleEnabled)
		    || !TryReadOptionalBool(
			    "SMTP Email Enabled (Y/N)   ",
			    GetConfigValue(configs, "email_smtp_enabled"),
			    out var emailSmtpEnabled))
		{
			return;
		}

		request.SchedulerIntervalHours = schedulerIntervalHours;
		request.MaxWeeklyHours = maxWeeklyHours;
		request.EmailConsoleEnabled = emailConsoleEnabled;
		request.EmailSmtpEnabled = emailSmtpEnabled;

		var response = await _adminClient.UpdateSystemConfigAsync(request);
		if (!response.Success || response.Data == null)
		{
			WriteApiError(response);
			return;
		}

		ConsoleHelper.WriteSuccess("System configuration updated.");
		Console.WriteLine();
		WriteConfigTable(response.Data);
		ConsoleHelper.PressEnterToContinue();
	}

	private static bool TryReadOptionalInt(string label, string? currentValue, out int? value)
	{
		var input = ConsoleHelper.ReadOptionalUpdateInput(label, currentValue);
		if (string.IsNullOrWhiteSpace(input))
		{
			value = null;
			return true;
		}

		if (int.TryParse(input, out var parsed) && parsed > 0)
		{
			value = parsed;
			return true;
		}

		ConsoleHelper.WriteError("Value must be a positive whole number, or blank to keep unchanged.");
		ConsoleHelper.PressEnterToContinue();
		value = null;
		return false;
	}

	private static bool TryReadOptionalBool(string label, string? currentValue, out bool? value)
	{
		var input = ConsoleHelper.ReadOptionalUpdateInput(label, currentValue);
		if (string.IsNullOrWhiteSpace(input))
		{
			value = null;
			return true;
		}

		if (input.Equals("Y", StringComparison.OrdinalIgnoreCase)
		    || input.Equals("YES", StringComparison.OrdinalIgnoreCase)
		    || input.Equals("TRUE", StringComparison.OrdinalIgnoreCase)
		    || input == "1")
		{
			value = true;
			return true;
		}

		if (input.Equals("N", StringComparison.OrdinalIgnoreCase)
		    || input.Equals("NO", StringComparison.OrdinalIgnoreCase)
		    || input.Equals("FALSE", StringComparison.OrdinalIgnoreCase)
		    || input == "0")
		{
			value = false;
			return true;
		}

		ConsoleHelper.WriteError("Value must be Y/N, true/false, 1/0, or blank to keep unchanged.");
		ConsoleHelper.PressEnterToContinue();
		value = null;
		return false;
	}

	private static string? GetConfigValue(IEnumerable<SystemConfigItem> configs, string key)
	{
		return configs.FirstOrDefault(config => config.Key.Equals(key, StringComparison.OrdinalIgnoreCase))?.Value;
	}

	private static void WriteConfigTable(IEnumerable<SystemConfigItem> configs)
	{
		Console.WriteLine($"{"Key",-34} {"Value",-24} {"Configured",-10} Updated");
		Console.WriteLine(new string('─', 92));

		foreach (var config in configs)
		{
			var value = config.IsSecret && config.IsConfigured ? "****" : config.Value;
			var configured = config.IsConfigured ? "Yes" : "No";
			Console.WriteLine(
				$"{Truncate(config.Key, 34),-34} {Truncate(value, 24),-24} {configured,-10} {config.UpdatedAt:dd-MMM-yyyy HH:mm}");

			if (!string.IsNullOrWhiteSpace(config.Description))
			{
				Console.WriteLine($"  {config.Description}");
			}
		}
	}

	private static string Truncate(string? value, int maxLength)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}

		return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
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
