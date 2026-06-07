using PRM.Client.Helpers;

namespace PRM.Client.Screens.Manager;

public class ManagerMenuScreen
{
	public void Show()
	{
		var running = true;

		while (running)
		{
			Console.Clear();
			ConsoleHelper.WriteHeader($"Manager Menu — Welcome, {SessionStore.FullName}");
			Console.WriteLine("  1. Resource Dashboard (Phase 4)");
			Console.WriteLine("  2. Allocate Resource (Phase 4)");
			Console.WriteLine("  3. My Projects (Phase 6)");
			Console.WriteLine("  4. Team Timesheets (Phase 6)");
			Console.WriteLine("  5. AI Assistant (Phase 8)");
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
