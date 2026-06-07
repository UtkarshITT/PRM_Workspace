using PRM.Client.Helpers;

namespace PRM.Client.Screens.Employee;

public class EmployeeMenuScreen
{
	public void Show()
	{
		var running = true;

		while (running)
		{
			Console.Clear();
			ConsoleHelper.WriteHeader($"Employee Menu — Welcome, {SessionStore.FullName}");
			Console.WriteLine("  1. Submit Timesheet (Phase 5)");
			Console.WriteLine("  2. View My Timesheets (Phase 5)");
			Console.WriteLine("  3. View My Allocations (Phase 5)");
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
