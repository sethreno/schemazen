using System;
using System.IO;
using System.Linq;
using SchemaZen.model;

namespace SchemaZen.console {
	public class Script : DbCommand {
		public Script()
			: base(
				"Script", "Generate scripts for the specified database.") {
			HasOption(
				"dataTables=",
				"A comma separated list of tables to export data from.",
				o => DataTables = o);
			HasOption(
				"dataTablesPattern=",
				"A regular expression pattern that matches tables to export data from.",
				o => DataTablesPattern = o);
			HasOption(
				"tableHint=",
				"Table hint to use when exporting data.",
				o => TableHint = o);
			HasOption("v|verbose=",
				"Verbose output of actions.",
				o => Verbose = o != null);
		}

		protected string DataTables { get; set; }
		protected string DataTablesPattern { get; set; }
		protected string TableHint { get; set; }
		protected bool Verbose { get; set; }

		public override int Run(string[] args) {
			var db = CreateDatabase();
			db.Load();

			if (!string.IsNullOrEmpty(DataTables)) {
				HandleDataTables(db, DataTables);
			}
			if (!string.IsNullOrEmpty(DataTablesPattern)) {
				var tables = db.FindTablesRegEx(DataTablesPattern);
				foreach (var t in tables.Where(t => !db.DataTables.Contains(t))) {
					db.DataTables.Add(t);
				}
			}

			if (!Overwrite && Directory.Exists(db.Dir)) {
				if (!ConsoleQuestion.AskYN(string.Format("{0} already exists - do you want to replace it", db.Dir)))
					return 1;
			}

			db.ScriptToDir(TableHint, VerboseOutput);

			Console.WriteLine("Snapshot successfully created at " + db.Dir);
			var routinesWithWarnings = db.Routines.Select(r => new {
				Routine = r,
				Warnings = r.Warnings().ToList()
			}).Where(r => r.Warnings.Any()).ToList();
			if (routinesWithWarnings.Any()) {
				Console.WriteLine("With the following warnings:");
				foreach (
					var warning in
						routinesWithWarnings.SelectMany(
							r =>
								r.Warnings.Select(
									w => string.Format("- {0} [{1}].[{2}]: {3}", r.Routine.RoutineType, r.Routine.Owner, r.Routine.Name, w)))) {
					Console.WriteLine(warning);
				}
			}
			return 0;
		}

		private void VerboseOutput (string actionType, string objectType, int progress, int total) {
			if (Verbose) {
				switch (actionType) {
					case "complete":
						Console.WriteLine(); // blank line to separate progress from completion message
						break;
					case "script":
						var output = string.Format("Scripting {0} {1} of {2}...", objectType, progress, total);
						if (progress < total)
							Console.Write(output + "\r");
						else
							Console.WriteLine(output);
						
						break;
					case "data":
						Console.WriteLine("Exporting data from {0} (table {1} of {2})...", objectType, progress, total);
						break;
				}
			}
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
