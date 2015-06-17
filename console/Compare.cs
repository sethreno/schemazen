using System;
using ManyConsole;
using model;
using NDesk.Options;

namespace console
{
	internal class Compare : ConsoleCommand
	{

		private string _source;
		private string _target;

		public Compare()
		{
			this.IsCommand("Compare", "Compare two databases.");
			this.Options = new OptionSet();
			this.SkipsCommandSummaryBeforeRunning();
			this.HasRequiredOption(
				"s|source=",
				"Connection string to a database to compare.",
				o => this._source = o);
			this.HasRequiredOption(
				"t|target=",
				"Connection string to a database to compare.",
				o => this._target = o);
		}

		public override int Run(string[] remainingArguments)
		{
			var sourceDb = new Database();
			var targetDb = new Database();
			sourceDb.Connection = this._source;
			targetDb.Connection = this._target;
			sourceDb.Load();
			targetDb.Load();
			var diff = sourceDb.Compare(targetDb);
			if (diff.IsDiff)
			{
				Console.WriteLine("Databases are different.");
				return 1;
			}
			Console.WriteLine("Databases are identical.");
			return 0;
		}
	}
}