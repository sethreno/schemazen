using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SchemaZen.Library.Command {
	public class ScriptCommand : BaseCommand {
		public void Execute(Dictionary<string, string> dataTableNames, string dataTablesPattern,
			string dataTablesExcludePattern,
			string tableHint, List<string> filteredTypes, List<string> schemas) {
			if (!Overwrite && Directory.Exists(ScriptDir)) {
				var message = $"{ScriptDir} already exists - you must set overwrite to true";
				throw new InvalidOperationException(message);
			}

			var db = CreateDatabase(filteredTypes, schemas);

			Logger.Log(TraceLevel.Verbose, "Loading database schema...");
			db.Load(Timeout);
			Logger.Log(TraceLevel.Verbose, "Database schema loaded.");

			foreach (var nameAndSchema in dataTableNames) {
				AddDataTable(db, nameAndSchema.Key, nameAndSchema.Value);
			}

			if (!string.IsNullOrEmpty(dataTablesPattern) ||
				!string.IsNullOrEmpty(dataTablesExcludePattern)) {
				var tables = db.FindTablesRegEx(dataTablesPattern, dataTablesExcludePattern);
				foreach (var t in tables.Where(t => !db.DataTables.Contains(t))) {
					db.DataTables.Add(t);
				}
			}

			db.ScriptToDir(tableHint, Logger.Log);

			Logger.Log(TraceLevel.Info,
				$"{Environment.NewLine}Snapshot successfully created at {db.Dir}");
			var routinesWithWarnings = db.Routines.Select(r => new {
				Routine = r,
				Warnings = r.Warnings().ToList()
			}).Where(r => r.Warnings.Any()).ToList();
			if (routinesWithWarnings.Any()) {
				Logger.Log(TraceLevel.Info, "With the following warnings:");
				var warnings = routinesWithWarnings.SelectMany(r => r.Warnings.Select(
					w => $"- {r.Routine.RoutineType} [{r.Routine.Owner}].[{r.Routine.Name}]: {w}"));
				foreach (var warning in warnings) {
					Logger.Log(TraceLevel.Warning, warning);
				}
			}
		}
	}
}
