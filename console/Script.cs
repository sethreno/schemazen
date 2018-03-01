using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ManyConsole;
using SchemaZen.Library;
using SchemaZen.Library.Command;
using SchemaZen.Library.Models;

namespace SchemaZen.console {
	public class Script : BaseCommand {
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
				"dataTablesExcludePattern=",
				"A regular expression pattern that exclude tables to export data from.",
				o => DataTablesExcludePattern = o);
			HasOption(
				"tableHint=",
				"Table hint to use when exporting data.",
				o => TableHint = o);
			HasOption(
				"filterTypes=",
				"A comma separated list of the types that will not be scripted. Valid types: " + Database.ValidTypes,
				o => FilterTypes = o);
			HasOption(
				"onlyTypes=",
				"A comma separated list of the types that will only be scripted. Valid types: " + Database.ValidTypes,
				o => OnlyTypes = o);
		}

		private Logger _logger;
		protected string DataTables { get; set; }
		protected string FilterTypes { get; set; }
		protected string OnlyTypes { get; set; }
		protected string DataTablesPattern { get; set; }
		protected string DataTablesExcludePattern { get; set; }
		protected string TableHint { get; set; }

		public override int Run(string[] args) {
			_logger = new Logger(Verbose);

			if (!Overwrite && Directory.Exists(ScriptDir)) {
				if (!ConsoleQuestion.AskYN($"{ScriptDir} already exists - do you want to replace it"))
					return 1;
			}

			var scriptCommand = new ScriptCommand {
				ConnectionString = ConnectionString,
				DbName = DbName,
				Pass = Pass,
				ScriptDir = ScriptDir,
				Server = Server,
				User = User,
				Logger = _logger,
				Overwrite = Overwrite
			};

			var filteredTypes = HandleFilteredTypes() ?? HandleOnlyTypes();
			var namesAndSchemas = HandleDataTables(DataTables);

			try {
				scriptCommand.Execute(namesAndSchemas, DataTablesPattern, DataTablesExcludePattern, TableHint, filteredTypes);
			} catch (Exception ex) {
				throw new ConsoleHelpAsException(ex.Message);
			}
			return 0;
		}

		private List<string> HandleFilteredTypes() {
			var filteredTypes = FilterTypes?.Split(',').ToList() ?? new List<string>();

			var anyInvalidType = false;
			foreach (var filterType in filteredTypes) {
				if (!Database.Dirs.Contains(filterType)) {
					_logger.Log(TraceLevel.Warning, $"{filterType} is not a valid type.");
					anyInvalidType = true;
				}
			}

			if (anyInvalidType) {
				_logger.Log(TraceLevel.Warning, $"Valid types: {Database.ValidTypes}");
			}

			return filteredTypes;
		}

		private List<string> HandleOnlyTypes() {
			var onlyTypes = OnlyTypes?.Split(',').ToList() ?? new List<string>(Database.Dirs);

			var anyInvalidType = false;
			foreach (var onlyType in onlyTypes) {
				if (!Database.Dirs.Contains(onlyType)) {
					_logger.Log(TraceLevel.Warning, $"{onlyType} is not a valid type.");
					anyInvalidType = true;
				}
			}

			if (anyInvalidType) {
				_logger.Log(TraceLevel.Warning, $"Valid types: {Database.ValidTypes}");
			}

			return Database.Dirs.Except(onlyTypes);
		}

		private Dictionary<string, string> HandleDataTables(string tableNames) {
			var dataTables = new Dictionary<string, string>();

			if (string.IsNullOrEmpty(tableNames))
				return dataTables;

			foreach (var value in tableNames.Split(',')) {
				var schema = "dbo";
				var name = value;
				if (value.Contains(".")) {
					schema = value.Split('.')[0];
					name = value.Split('.')[1];
				}

				dataTables[name] = schema;
			}

			return dataTables;
		}
	}
}
