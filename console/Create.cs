using System;
using System.IO;
using model;

namespace console {
	public class Create : DbCommand {
		public Create()
			: base(
				"Create", "Create the specified database from scripts.") { }

		public override int Run(string[] remainingArguments) {
			Database db = CreateDatabase();
			if (!Directory.Exists(db.Dir)) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Snapshot dir {0} does not exist.", db.Dir);
				Console.ForegroundColor = ConsoleColor.Black;
				return 1;
			}

			if (DBHelper.DbExists(db.Connection) && !Overwrite) {
				if (!ConsoleQuestion.AskYN(string.Format("{0} {1} already exists - do you want to drop it", Server, DbName))) {
					Console.WriteLine("Create command cancelled.");
					return 1;
				}
				Overwrite = true;
			}

			try {
				db.CreateFromDir(Overwrite);
				Console.WriteLine();
				Console.WriteLine("Database created successfully.");
			} catch (BatchSqlFileException ex) {
				Console.WriteLine();
				Console.WriteLine(@"Create completed with the following errors:");
				Console.ForegroundColor = ConsoleColor.Red;
				foreach (SqlFileException e in ex.Exceptions) {
					Console.WriteLine(@"{0} (Line {1}): {2}", e.FileName.Replace("/", "\\"), e.LineNumber, e.Message);
				}
				Console.ForegroundColor = ConsoleColor.Black;
				return -1;
			} catch (SqlFileException ex) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write(@"An unexpected SQL error occurred while executing scripts, and the process wasn't completed.
{0} (Line {1}): {2}", ex.FileName.Replace("/", "\\"), ex.LineNumber, ex.Message);
				Console.ForegroundColor = ConsoleColor.Black;
				return -1;
			}

			return 0;
		}
	}
}
