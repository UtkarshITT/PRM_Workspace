namespace PRM.Client.Helpers;

public static class ConsoleHelper
{
	private const int BoxWidth = 46;

	public static void WriteHeader(string title)
	{
		Console.WriteLine();
		Console.WriteLine("╔" + new string('═', BoxWidth) + "╗");
		Console.WriteLine($"║    {TrimToWidth(title.ToUpperInvariant(), BoxWidth - 4).PadRight(BoxWidth - 4)}║");
		Console.WriteLine("╚" + new string('═', BoxWidth) + "╝");
		Console.WriteLine();
	}

	public static void WriteAppHeader()
	{
		Console.WriteLine();
		Console.WriteLine("╔" + new string('═', BoxWidth) + "╗");
		Console.WriteLine($"║    {"PROJECT & RESOURCE MANAGEMENT TOOL".PadRight(BoxWidth - 4)}║");
		Console.WriteLine($"║    {"Learn & Code — Final Project".PadRight(BoxWidth - 4)}║");
		Console.WriteLine("╚" + new string('═', BoxWidth) + "╝");
		Console.WriteLine();
	}

	public static void WriteWelcomeHeader(string title, string? name = null, bool includeTime = true)
	{
		var welcome = string.IsNullOrWhiteSpace(name)
			? title
			: $"Welcome, {name}!{(includeTime ? $"  |  {DateTime.Now:dd-MMM-yyyy  HH:mm}" : string.Empty)}";

		Console.WriteLine();
		Console.WriteLine("╔" + new string('═', BoxWidth) + "╗");
		Console.WriteLine($"║    {TrimToWidth(title.ToUpperInvariant(), BoxWidth - 4).PadRight(BoxWidth - 4)}║");
		Console.WriteLine($"║    {TrimToWidth(welcome, BoxWidth - 4).PadRight(BoxWidth - 4)}║");
		Console.WriteLine("╚" + new string('═', BoxWidth) + "╝");
		Console.WriteLine();
	}

	public static void WriteDivider()
	{
		Console.WriteLine(new string('─', BoxWidth));
	}

	public static void WriteKeepCurrentHint()
	{
		Console.WriteLine("Press Enter to keep the current value.");
		Console.WriteLine("For optional text fields, enter - to clear the value.");
		Console.WriteLine();
	}

	public static void WriteError(string message)
	{
		Console.ForegroundColor = ConsoleColor.Red;
		Console.WriteLine(message);
		Console.ResetColor();
	}

	public static void WriteSuccess(string message)
	{
		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine(message);
		Console.ResetColor();
	}

	public static string ReadInput(string prompt, bool secret = false)
	{
		Console.Write(prompt);

		if (!secret)
		{
			return Console.ReadLine()?.Trim() ?? string.Empty;
		}

		var password = string.Empty;
		ConsoleKeyInfo key;

		do
		{
			key = Console.ReadKey(intercept: true);

			if (key.Key == ConsoleKey.Backspace && password.Length > 0)
			{
				password = password[..^1];
				Console.Write("\b \b");
				continue;
			}

			if (!char.IsControl(key.KeyChar))
			{
				password += key.KeyChar;
				Console.Write('*');
			}
		}
		while (key.Key != ConsoleKey.Enter);

		Console.WriteLine();
		return password;
	}

	public static string ReadOptionalUpdateInput(string label, string? currentValue, bool secret = false)
	{
		var displayValue = string.IsNullOrWhiteSpace(currentValue) ? "-" : currentValue;
		return ReadInput($"{label} [{displayValue}]: ", secret);
	}

	public static void PressEnterToContinue()
	{
		Console.WriteLine();
		Console.Write("Press Enter to continue...");
		Console.ReadLine();
	}

	private static string TrimToWidth(string value, int width)
	{
		return value.Length <= width ? value : value[..width];
	}
}
