using System;
using System.Diagnostics;
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
		}

		protected string DataTables { get; set; }
		protected string DataTablesPattern { get; set; }
		protected string TableHint { get; set; }

		public override int Run(string[] args) {
			if (!Overwrite && Directory.Exists(ScriptDir)) {
				if (!ConsoleQuestion.AskYN(string.Format("{0} already exists - do you want to replace it", ScriptDir)))
					return 1;
			}

			var db = CreateDatabase();
			Log(TraceLevel.Verbose, "Loading database schema...");
			db.Load();
			Log(TraceLevel.Verbose, "Database schema loaded.");

			if (!string.IsNullOrEmpty(DataTables))
			{
				HandleDataTables(db, DataTables);
			}
			if (!string.IsNullOrEmpty(DataTablesPattern))
			{
				var tables = db.FindTablesRegEx(DataTablesPattern);
				foreach (var t in tables.Where(t => !db.DataTables.Contains(t)))
				{
					db.DataTables.Add(t);
				}
			}

			db.ScriptToDir(Log, TableHint);

			Log(TraceLevel.Info, "Snapshot successfully created at " + db.Dir);
			var routinesWithWarnings = db.Routines.Select(r => new {
				Routine = r,
				Warnings = r.Warnings().ToList()
			}).Where(r => r.Warnings.Any()).ToList();
			if (routinesWithWarnings.Any()) {
				Log(TraceLevel.Info, "With the following warnings:");
				foreach (
					var warning in
						routinesWithWarnings.SelectMany(
							r =>
								r.Warnings.Select(
									w => string.Format("- {0} [{1}].[{2}]: {3}", r.Routine.RoutineType, r.Routine.Owner, r.Routine.Name, w)))) {
					Log(TraceLevel.Warning, warning);
				}
			}
			return 0;
		}

		private void HandleDataTables(Database db, string tableNames) {
			foreach (var value in tableNames.Split(',')) {
				var schema = "dbo";
				var name = value;
				if (value.Contains(".")) {
					schema = value.Split('.')[0];
					name = value.Split('.')[1];
				}
				var t = db.FindTable(name, schema);
				if (t == null) {
					Log(TraceLevel.Warning, string.Format(
						"warning: could not find data table {0}.{1}",
						schema, name));
				}
				if (db.DataTables.Contains(t)) continue;
				db.DataTables.Add(t);
			}
		}
	}
}
