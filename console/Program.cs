using System;
using System.Collections.Generic;

namespace console {
	internal class Program {
		private static int Main(string[] args) {
			ICommand cmd = new Default();
			if (args.Length > 0) {
				var commands = new Dictionary<string, ICommand>();
				commands.Add("script", new Script());
				commands.Add("create", new Create());
				commands.Add("compare", new Compare());
				if (commands.ContainsKey(args[0].ToLower())) {
					cmd = commands[args[0].ToLower()];
				}
			}

			if (!cmd.Parse(args)) {
				Console.WriteLine("schemazen");
				Console.WriteLine("Copyright (c) Seth Reno. All rights reserved.");
				Console.WriteLine();
				Console.Write("usage: schemazen " + cmd.GetUsageText());
				return -1;
			}
			if (!cmd.Run()) {
				return -1;
			}
			return 0;
		}
	}
}