using System;
using System.IO;
using ManyConsole;
using model;
using NDesk.Options;

namespace console {
	public class Script : ConsoleCommand {
		private string _conn;
		private string _dir;
		private bool _overwrite;

		public Script() {
			IsCommand("Script", "Generate scripts for the specified database.");
			Options = new OptionSet();
			SkipsCommandSummaryBeforeRunning();
			HasRequiredOption(
			  "c|conn=",
			  "Connection string to a database to script.",
			  o => _conn = o);
			HasRequiredOption(
			  "d|dir=",
			  "Path to the output directory where scripts will be created.",
			  o => _dir = o);
			HasOption(
			  "o|overwrite=",
			  "Overwrite existing scripts without prompt.",
			  o => _overwrite = o != null);
		}

		public override int Run(string[] args) {
			// load the model
			var db = new Database() {Connection = _conn, Dir = _dir};
			db.Load();

			// generate scripts
			if (!_overwrite && Directory.Exists(db.Dir)) {
				Console.Write("{0} already exists do you want to replace it? (Y/N)", db.Dir);
				var key = Console.ReadKey();
				if (key.Key != ConsoleKey.Y) {
					return 1;
				}
				Console.WriteLine();
			}

			db.ScriptToDir(_overwrite);

			Console.WriteLine("Snapshot successfully created at " + db.Dir);
			return 0;
		}
	}
}