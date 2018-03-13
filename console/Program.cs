using System;
using System.Collections.Generic;
using System.Diagnostics;
using ManyConsole;


namespace SchemaZen.console {
	internal class Program {
		private static int Main(string[] args) {
			//SqlServerTypes.Utilities.LoadNativeAssemblies(Microsoft.SqlServer.Server.MapPath("~"));
			//SqlServerTypes.Utilities.LoadNativeAssemblies(Server.MapPath("~/bin"));
			SqlServerTypes.Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory);
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
