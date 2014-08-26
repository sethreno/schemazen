using System;
using System.IO;
using System.Xml.Serialization;
using ManyConsole;
using model;
using NDesk.Options;

namespace console {
	internal class Compare : ConsoleCommand {

		private string _source;
		private string _target;

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
		}

		public override int Run(string[] remainingArguments) {
			var sourceDb = new Database();
			var targetDb = new Database();
			sourceDb.Connection = _source;
			targetDb.Connection = _target;
			sourceDb.Load();
			targetDb.Load();
			DatabaseDiff diff = sourceDb.Compare(targetDb, new CompareConfig());

			var serializer = new XmlSerializer(typeof(DatabaseDiff));
			using (var stream = new StreamWriter("diff.xml", false))
			{
				serializer.Serialize(stream, diff);
			}

			if (diff.IsDiff) {
				Console.WriteLine("Databases are different.");
				return 1;
			}
			Console.WriteLine("Databases are identical.");
			return 0;
		}
	}
}