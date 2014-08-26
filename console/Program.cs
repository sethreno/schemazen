using System;
using System.Collections.Generic;
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
			}
		}

		static IEnumerable<ConsoleCommand> GetCommands() {
			return new List<ConsoleCommand>() {
				new Script(), new Create(), new Compare(), new Dump()
			};
		}
	}
}