using System;
using System.IO;
using ManyConsole;
using model;
using NDesk.Options;

namespace console {
	internal class Compare : ConsoleCommand {
		private string _source;
		private string _target;
		private string _outDiff;
		private bool _overwrite;

		public Compare() {
			IsCommand("Compare", "Compare two databases.");
			Options = new OptionSet();
			SkipsCommandSummaryBeforeRunning();
			HasRequiredOption(
				"s|source=",
				"Connection string to a database to compare.",
				o => _source = o);
			HasRequiredOption(
				"t|target=",
				"Connection string to a database to compare.",
				o => _target = o);
			HasOption(
				"outFile=",
				"Create a sql diff file in the specified path.",
				o => _outDiff = o);
			HasOption(
				"o|overwrite=",
				"Overwrite existing target without prompt.",
				o => _overwrite = o != null);
		}

		public override int Run(string[] remainingArguments) {
			var sourceDb = new Database();
			var targetDb = new Database();
			sourceDb.Connection = _source;
			targetDb.Connection = _target;
			sourceDb.Load();
			targetDb.Load();
			DatabaseDiff diff = sourceDb.Compare(targetDb);
			if (diff.IsDiff) {
				Console.WriteLine("Databases are different.");
				if (!string.IsNullOrEmpty(_outDiff)) {
					if (!_overwrite && File.Exists(_outDiff)) {
						if (!ConsoleQuestion.AskYN(string.Format("{0} already exists - do you want to replace it", _outDiff))) {
							return 1;
						}
					}
					File.WriteAllText(_outDiff, diff.Script());
					Console.WriteLine("Script to make the databases identical has been created at {0}", Path.GetFullPath(_outDiff));
				}
				return 1;
			}
			Console.WriteLine("Databases are identical.");
			return 0;
		}
	}
}
