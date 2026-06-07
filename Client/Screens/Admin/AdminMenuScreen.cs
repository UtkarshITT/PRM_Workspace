using PRM.Client.Helpers;

namespace PRM.Client.Screens.Admin;

public class AdminMenuScreen
{
	public void Show()
	{
		var running = true;

		while (running)
		{
			Console.Clear();
			ConsoleHelper.WriteHeader($"Admin Menu — Welcome, {SessionStore.FullName}");
			Console.WriteLine("  1. Manage Users (Phase 2)");
			Console.WriteLine("  2. Manage Employees (Phase 2)");
			Console.WriteLine("  3. Manage Projects (Phase 3)");
			Console.WriteLine("  4. System Configuration (Phase 9)");
			Console.WriteLine("  0. Logout");
			Console.WriteLine();
			Console.Write("Select option: ");

			switch (Console.ReadLine()?.Trim())
			{
				case "0":
					SessionStore.Clear();
					running = false;
					break;
				default:
					ConsoleHelper.WriteError("Feature coming in a later phase.");
					ConsoleHelper.PressEnterToContinue();
					break;
			}
		}
	}
}
