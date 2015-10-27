using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SchemaZen.model;

namespace SchemaZen.console
{
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
		}

		protected string DataTables { get; set; }
		protected string DataTablesPattern { get; set; }
		protected string TableHint { get; set; }

		public override int Run(string[] args) {
			Database db = CreateDatabase();
			db.Load();

			if (!string.IsNullOrEmpty(DataTables)) {
				HandleDataTables(db, DataTables);
			}
			if (!string.IsNullOrEmpty(DataTablesPattern)) {
				List<Table> tables = db.FindTablesRegEx(DataTablesPattern);
				foreach (Table t in tables.Where(t => !db.DataTables.Contains(t))) {
					db.DataTables.Add(t);
				}
			}

			if (!Overwrite && Directory.Exists(db.Dir)) {
				if (!ConsoleQuestion.AskYN(string.Format("{0} already exists - do you want to replace it", db.Dir)))
					return 1;
			}

			db.ScriptToDir(TableHint);

			Console.WriteLine("Snapshot successfully created at " + db.Dir);
			var routinesWithWarnings = db.Routines.Select(r => new {
																	   Routine = r,
																	   Warnings = r.Warnings().ToList()
																   }).Where(r => r.Warnings.Any()).ToList();
			if (routinesWithWarnings.Any()) {
				Console.WriteLine("With the following warnings:");
				foreach (var warning in routinesWithWarnings.SelectMany(r => r.Warnings.Select(w => "- " + r.Routine.RoutineType.ToString() + " " + r.Routine.Name + ": " + w))) {
					Console.WriteLine(warning);
				}
			}
			return 0;
		}

		private static void HandleDataTables(Database db, string tableNames) {
			foreach (string value in tableNames.Split(',')) {
				string schema = "dbo";
				string name = value;
				if (value.Contains(".")) {
					schema = value.Split('.')[0];
					name = value.Split('.')[1];
				}
				Table t = db.FindTable(name, schema);
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
