﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using ManyConsole;

namespace SchemaZen.console {
	internal class Program {
		private static int Main(string[] args) {
			try {
				return ConsoleCommandDispatcher.DispatchCommand(
					GetCommands(), args, Console.Out);
			} catch (Exception ex) {
				Console.WriteLine(); // write empty line in case last character was a carriage return, for verbose logging
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
