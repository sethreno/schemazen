using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using ManyConsole;

namespace SchemaZen.console {
	internal class Program {
		private static int Main(string[] args) {
			string loc = Assembly.GetEntryAssembly().Location;
			string config = string.Concat(loc, ".config");
			if (!File.Exists(config)) {
				System.Text.StringBuilder sb = new StringBuilder();
				sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
				sb.AppendLine("<configuration>");
				sb.AppendLine("<startup useLegacyV2RuntimeActivationPolicy=\"true\"/>");
				sb.AppendLine("</configuration>");

				
				System.IO.File.WriteAllText(config, sb.ToString());
			}
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
