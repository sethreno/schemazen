using System;
using SystemConsole = System.Console;

namespace SchemaZen.Console;

public static class ConsoleQuestion {
	// ReSharper disable once InconsistentNaming
	public static bool AskYN(string question) {
		SystemConsole.Write($"{question} (Y/N)? ");

		ConsoleKeyInfo key;
		do {
			key = SystemConsole.ReadKey();
		} while (key.Key != ConsoleKey.Y && key.Key != ConsoleKey.N);

		SystemConsole.WriteLine();
		return key.Key == ConsoleKey.Y;
	}

	public static void WaitForKeyPress() {
		SystemConsole.WriteLine("Press any key to continue...");
		SystemConsole.ReadKey(true);
	}
}
