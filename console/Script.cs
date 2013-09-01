using System;
using System.IO;
using model;

namespace console {
	public class Script : ICommand {
		private DataArg data;
		private bool delete;
		private Operand destination;
		private Operand source;

		public bool Parse(string[] args) {
			if (args.Length < 3) {
				return false;
			}
			source = Operand.Parse(args[1]);
			if (!args[2].ToLower().StartsWith("dir:")) {
				args[2] = "dir:" + args[2];
			}
			destination = Operand.Parse(args[2]);
			data = DataArg.Parse(args);
			foreach (string arg in args) {
				if (arg.ToLower() == "-d") delete = true;
			}

			if (source == null || destination == null) return false;
			if (source.OpType != OpType.Database) return false;
			if (destination.OpType != OpType.ScriptDir) return false;

			return true;
		}

		public string GetUsageText() {
			return @"script <source> <destination> [-d]

Generate scripts for the specified database.

<source>                The connection string to the database to script.
              Example:
                ""server=localhost;database=DEVDB;Trusted_Connection=yes;""

<destination>           Path to the directory where scripts will be created

-d                      Delete existing scripts without prompt.
";
		}

		public bool Run() {
			// load the model
			var db = new Database();
			db.Connection = source.Value;
			db.Dir = destination.Value;
			db.Load();

			// generate scripts
			if (!delete && Directory.Exists(destination.Value)) {
				Console.Write("{0} already exists do you want to replace it? (Y/N)", destination.Value);
				ConsoleKeyInfo key = Console.ReadKey();
				if (key.Key != ConsoleKey.Y) {
					return false;
				}
				Console.WriteLine();
			}

			if (data != null) {
				foreach (string pattern in data.Value.Split(',')) {
					if (string.IsNullOrEmpty(pattern)) {
						continue;
					}
					foreach (Table t in db.FindTablesRegEx(pattern)) {
						db.DataTables.Add(t);
					}
				}
			}

			db.ScriptToDir(delete);

			Console.WriteLine("Snapshot successfully created at " + destination.Value);
			return true;
		}
	}
}