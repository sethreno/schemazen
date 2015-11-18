using System;
using System.IO;
using ManyConsole;
using NDesk.Options;
using SchemaZen.model;

namespace SchemaZen.console {
	internal class Compare : ConsoleCommand {
		private string _source;
		private string _target;
		private string _outDiff;
		private bool _overwrite;
		private bool _verbose;

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
			HasOption(
				"v|verbose=",
				"Enable verbose mode (show detailed changes).",
				o => _verbose = o != null);
		}

		public override int Run(string[] remainingArguments) {
			var sourceDb = new Database();
			var targetDb = new Database();
			sourceDb.Connection = _source;
			targetDb.Connection = _target;
			sourceDb.Load();
			targetDb.Load();
			var diff = sourceDb.Compare(targetDb);
			if (diff.IsDiff) {
				Console.WriteLine("Databases are different.");
				Console.WriteLine(diff.SummarizeChanges(_verbose));
				if (!string.IsNullOrEmpty(_outDiff)) {
					Console.WriteLine();
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
