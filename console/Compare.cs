using System;
using model;

namespace console {
	internal class Compare : ICommand {
		private Operand source;
		private Operand target;

		public string GetUsageText() {
			return @"compare <source> <target>

Compares two databases.

<source>                The connection strings to databases to compare. 
<target>                
              Example:
                ""server=localhost;database=DEVDB;Trusted_Connection=yes;""
";
		}

		public bool Parse(string[] args) {
			if (args.Length < 3) {
				return false;
			}
			source = Operand.Parse(args[1]);
			target = Operand.Parse(args[2]);

			if (source == null || target == null) return false;
			if (source.OpType != OpType.Database) return false;
			if (target.OpType != OpType.Database) return false;
			return true;
		}

		public Boolean Run() {
			var sourceDb = new Database();
			var targetDb = new Database();
			sourceDb.Connection = source.Value;
			targetDb.Connection = target.Value;
			sourceDb.Load();
			targetDb.Load();
			DatabaseDiff diff = sourceDb.Compare(targetDb);
			if (diff.IsDiff) {
				Console.WriteLine("Databases are different.");
				return false;
			}
			Console.WriteLine("Databases are identical.");
			return true;
		}
	}
}