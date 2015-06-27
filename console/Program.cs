using System;
using System.Collections.Generic;
using System.Diagnostics;
using ManyConsole;

namespace console {
	internal class Program {
		private static int Main(string[] args) {
			try {
				return ConsoleCommandDispatcher.DispatchCommand(
					GetCommands(), args, Console.Out);
			} catch (Exception ex) {
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.StackTrace);
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
}
