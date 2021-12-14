using System;
using System.Collections.Generic;
using System.Diagnostics;
using ManyConsole;
using SystemConsole = System.Console;

namespace SchemaZen.Console;

internal class Program {
	private static int Main(string[] args) {
		try {
			return ConsoleCommandDispatcher.DispatchCommand(
				GetCommands(),
				args,
				SystemConsole.Out);
		} catch (Exception ex) {
			SystemConsole.WriteLine(ex.Message);
			SystemConsole.WriteLine(ex.StackTrace);
			return -1;
		} finally {
#if DEBUG
			if (Debugger.IsAttached)
				ConsoleQuestion.WaitForKeyPress();
#endif
		}
	}

	private static IEnumerable<ConsoleCommand> GetCommands() {
		return new List<ConsoleCommand> {
			new Script(),
			new Create(),
			new Compare()
		};
	}
}
