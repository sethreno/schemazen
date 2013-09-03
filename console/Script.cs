using System;
using System.IO;

namespace console {
	public class Script : DbCommand {

		public Script() : base(
		  "Script", "Generate scripts for the specified database.") { }

		public override int Run(string[] args) {
			var db = CreateDatabase();
			db.Load();

			if (!Overwrite && Directory.Exists(db.Dir)) {
				Console.Write("{0} already exists do you want to replace it? (Y/N)", db.Dir);
				var key = Console.ReadKey();
				if (key.Key != ConsoleKey.Y) {
					return 1;
				}
				Console.WriteLine();
			}

			db.ScriptToDir(Overwrite);

			Console.WriteLine("Snapshot successfully created at " + db.Dir);
			return 0;
		}
	}
}