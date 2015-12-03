using System;
using System.Diagnostics;
using System.IO;
using SchemaZen.model;

namespace SchemaZen.console {
	public class Create : DbCommand {
		public Create () : base("Create", "Create the specified database from scripts.") { }

		public override int Run(string[] remainingArguments) {
			var db = CreateDatabase();
			if (!Directory.Exists(db.Dir)) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Snapshot dir {0} does not exist.", db.Dir);
				Console.ForegroundColor = ConsoleColor.White;
				return 1;
			}

			Log(TraceLevel.Verbose, "Checking if database already exists...");
			if (DBHelper.DbExists(db.Connection) && !Overwrite) {
				if (!ConsoleQuestion.AskYN(string.Format("{0} {1} already exists - do you want to drop it", Server, DbName))) {
					Log(TraceLevel.Info, "Create command cancelled.");
					return 1;
				}
				Overwrite = true;
			}

			var prevColor = Console.ForegroundColor;

			try {
				db.CreateFromDir(Overwrite, Log);
				Log(TraceLevel.Info, string.Empty);
				Log(TraceLevel.Info, "Database created successfully.");
			} catch (BatchSqlFileException ex) {
				Console.WriteLine();
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Create completed with the following errors:");
				Console.ForegroundColor = prevColor;
				foreach (var e in ex.Exceptions)
				{
					Console.WriteLine("- {0} (Line {1}):", e.FileName.Replace("/", "\\"), e.LineNumber);
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine(" {0}", e.Message);
					Console.ForegroundColor = prevColor;
				}

				return -1;
			} catch (SqlFileException ex) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write(@"An unexpected SQL error occurred while executing scripts, and the process wasn't completed.
{0} (Line {1}): {2}", ex.FileName.Replace("/", "\\"), ex.LineNumber, ex.Message);
				Console.ForegroundColor = prevColor;
				return -1;
			}

			return 0;
		}
	}
}
