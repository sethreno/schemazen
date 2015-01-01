using System;
using System.IO;
using System.Security.Cryptography;
using model;

namespace console {
	public class Script : DbCommand {

		protected string DataTables { get; set; }
		protected string DataTablesPattern { get; set; }

		public Script() : base(
			"Script", "Generate scripts for the specified database.") {
			HasOption(
				"dataTables=",
				"A comma separated list of tables to export data from.",
				o => DataTables = o);
			HasOption(
				"dataTablesPattern=",
				"A regular expression pattern that matches tables to export data from.",
				o => DataTablesPattern = o);
		}

		public override int Run(string[] args) {
			var db = CreateDatabase();
			db.Load();

			if (!String.IsNullOrEmpty(DataTables)) {
				HandleDataTables(db, DataTables);
			}
			if (!String.IsNullOrEmpty(DataTablesPattern)) {
				var tables = db.FindTablesRegEx(DataTablesPattern);
				foreach (var t in tables) {
					if (db.DataTables.Contains(t)) continue;
					db.DataTables.Add(t);
				}
			}

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

		private static void HandleDataTables(Database db, string tableNames) {
			foreach (var value in tableNames.Split(',')) {
				var schema = "dbo";
				var name = value;
				if (value.Contains(".")) {
					schema = value.Split('.')[0];
					name = value.Split('.')[1];
				}
				var t = db.FindTable(name, schema);
				if (t == null) {
					Console.WriteLine(
						"warning: could not find data table {0}.{1}",
						schema, name);
				}
				if (db.DataTables.Contains(t)) continue;
				db.DataTables.Add(t);
			}
		}
	}
}