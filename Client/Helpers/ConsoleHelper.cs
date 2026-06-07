namespace PRM.Client.Helpers;

public static class ConsoleHelper
{
	public static void WriteHeader(string title)
	{
		Console.WriteLine();
		Console.WriteLine(new string('=', title.Length + 4));
		Console.WriteLine($"  {title}  ");
		Console.WriteLine(new string('=', title.Length + 4));
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

	public static void PressEnterToContinue()
	{
		Console.WriteLine();
		Console.Write("Press Enter to continue...");
		Console.ReadLine();
	}
}
