using System;
using System.IO;
using System.Xml.Serialization;
using ManyConsole;
using model;
using model.compare;
using NDesk.Options;

namespace console {
	internal class Compare : ConsoleCommand {

		private string _source;
		private string _target;

		private string _sourceDump;
		private string _targetDump;

		private bool _debug;

		private bool _ignoreProps;
		private bool _ignoreDefaultsNameMismatch;
		private bool _ignoreRoutinesTextMismatch;
		private bool _ignoreConstraintsNameMismatch;

		private string _compareMethod;
		private CompareMethod _selectedCompareMethod = CompareMethod.FindAllDifferences;

		public Compare() {
			IsCommand("Compare", "Compare two databases.");
			Options = new OptionSet();
			SkipsCommandSummaryBeforeRunning();
			HasOption(
				"s|source=",
				"Connection string to a database to compare.",
				o => _source = o);
			HasOption(
				"t|target=",
				"Connection string to a database to compare.",
				o => _target = o);
			HasOption(
				"x|sourceDump=",
				"File path to a database dump to compare.",
				o => _sourceDump = o);
			HasOption(
				"c|targetDump=",
				"File path to a database dump to compare.",
				o => _targetDump = o);
			HasOption(
				"CompareMethod=",
				"There are two possible values. \n \"FindAllDifferences\" (default) shows all differences between the two databases. \n \"FindButIgnoreAdditionalItems\" does ignore additional items (routines, tables, columns, constraints) of the target database ",
				o => _compareMethod = o);
			HasOption(
				"d|debug",
				"Creates a diff.xml file with debug details.",
				o => _debug = o != null);
			HasOption(
				"IgnoreProps",
				"Ignores properties while comparing database.",
				o => _ignoreProps = o != null);
			HasOption(
				"IgnoreDefaultsNameMismatch",
				"Ignores different names of default constraints while comparing database.",
				o => _ignoreDefaultsNameMismatch = o != null);
			HasOption(
				"IgnoreRoutinesTextMismatch",
				"Ignores different text in routines while comparing database.",
				o => _ignoreRoutinesTextMismatch = o != null);
			HasOption(
				"IgnoreConstraintsNameMismatch",
				"Ignores different names of constraints while comparing database.",
				o => _ignoreConstraintsNameMismatch = o != null);
		}

		public override int Run(string[] remainingArguments) {
			var sourceDb = GetSourceDb();
			var targetDb = GetTargetDb();

			DatabaseDiff diff = sourceDb.Compare(targetDb, new CompareConfig
			{
				IgnoreProps = _ignoreProps,
				IgnoreConstraintsNameMismatch = _ignoreConstraintsNameMismatch,
				IgnoreDefaultsNameMismatch = _ignoreDefaultsNameMismatch,
				IgnoreRoutinesTextMismatch = _ignoreRoutinesTextMismatch,
				ColumnsCompareMethod = _selectedCompareMethod,
				ConstraintsCompareMethod = _selectedCompareMethod,
				ForeignKeysCompareMethod = _selectedCompareMethod,
				RoutinesCompareMethod = _selectedCompareMethod,
				TablesCompareMethod = _selectedCompareMethod
			});
			var diffreport = diff.GetDiffReport();

			if (_debug) {
				var serializer = new XmlSerializer(typeof(DiffReport));
				using (var stream = new StreamWriter("diff.xml", false)) {
					serializer.Serialize(stream, diffreport);
				}
			}

			if (diff.IsDiff) {
				Console.WriteLine(diffreport.Script());
				return 1;
			}
			Console.WriteLine("Databases are identical.");
			return 0;
		}

		private Database GetSourceDb() {
			return GetDatabase(_source, _sourceDump);
		}

		private Database GetTargetDb() {
			return GetDatabase(_target, _targetDump);
		}

		private Database GetDatabase(string connectionString, string filePath) {
			Database db;
			if (!string.IsNullOrEmpty(connectionString)) {
				db = new Database();
				db.Connection = connectionString;
				db.Load();

				return db;
			}

			var serializer = new XmlSerializer(typeof(Database));
			using (var stream = new StreamReader(filePath, false)) {
				return (Database)serializer.Deserialize(stream);
			}
		}

		public override void CheckRequiredArguments()
		{
			if (string.IsNullOrEmpty(_source) && string.IsNullOrEmpty(_sourceDump) && string.IsNullOrEmpty(_target) &&
				string.IsNullOrEmpty(_targetDump))
				throw new ConsoleHelpAsException("You have to specify at least source or sourceDump and target oder targetDump.");

			if (!(string.IsNullOrEmpty(_source) ^ string.IsNullOrEmpty(_sourceDump)))
				throw new ConsoleHelpAsException("You have to specify a connectionstring or a file path as source using 'source=' or 'sourceDump='");

			if (!(string.IsNullOrEmpty(_target) ^ string.IsNullOrEmpty(_targetDump)))
				throw new ConsoleHelpAsException("You have to specify a connectionstring or a file path as target using 'target=' or 'targetDump='");

			if (!string.IsNullOrEmpty(_compareMethod))
				if (!Enum.TryParse(_compareMethod, true, out _selectedCompareMethod))
					throw new ConsoleHelpAsException(string.Format("Unknown compare method: {0}", _compareMethod));

			base.CheckRequiredArguments();
		}
	}
}