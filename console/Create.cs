using System;
using System.IO;
using model;

namespace console {
	public class Create : DbCommand {

		public Create() : base(
		  "Create", "Create the specified database from scripts.") { }

		public override int Run(string[] remainingArguments) {
			var db = CreateDatabase();
			if (!Directory.Exists(db.Dir)){
				Console.WriteLine("Snapshot dir {0} does not exist.", db.Dir);
				return 1;
			}

			if (DBHelper.DbExists(db.Connection) && !Overwrite) {
				Console.WriteLine("{0} {1} already exists do you want to drop it? (Y/N)",
					Server, DbName); 

				var answer = char.ToUpper(Convert.ToChar(Console.Read()));
				while (answer != 'Y' && answer != 'N') {
					answer = char.ToUpper(Convert.ToChar(Console.Read()));
				}
				if (answer == 'N') {
					Console.WriteLine("create command cancelled.");
					return 1;
				}
				Overwrite = true;
			}

			try {
				db.CreateFromDir(Overwrite);
				Console.WriteLine("Database created successfully.");
			}
			catch (BatchSqlFileException ex) {
				Console.WriteLine(@"Create completed with the following errors:");
				foreach (SqlFileException e in ex.Exceptions) {
					Console.WriteLine(@"{0}(Line {1}): {2}",
						e.FileName.Replace("/", "\\"), e.LineNumber, e.Message);
				}
			}
			catch (SqlFileException ex) {
				Console.Write(@"A SQL error occurred while executing scripts.
{0}(Line {1}): {2}", ex.FileName.Replace("/", "\\"), ex.LineNumber, ex.Message);
				return -1;
			}

			return 0;
		}
	}
}