using System;
using System.Data.SqlClient;
using System.IO;
using ManyConsole;
using model;
using NDesk.Options;

namespace console {
	public class Create : ConsoleCommand {

		private string _conn;
		private string _dir;
		private bool _overwrite;

		public Create() {
			IsCommand("Create", "Create the specified database from scripts.");
			Options = new OptionSet();
			SkipsCommandSummaryBeforeRunning();
			HasRequiredOption(
				"c|conn=",
				"Connection string to a database to create.",
				o => _conn = o);
			HasRequiredOption(
				"d|dir=",
				"Path to the output directory where scripts will be created.",
				o => _dir = o);
			HasOption(
				"o|overwrite=",
				"Overwrite existing database without prompt.",
				o => _overwrite = o != null);
		}

		public override int Run(string[] remainingArguments) {
			if (!Directory.Exists(_dir)){
				Console.WriteLine("Snapshot dir {0} does not exist.", _dir);
				return 1;
			}

			if (DBHelper.DbExists(_conn) && !_overwrite) {
				var cnBuilder = new SqlConnectionStringBuilder(_conn);
				Console.WriteLine("{0} {1} already exists do you want to drop it? (Y/N)",
					cnBuilder.DataSource, cnBuilder.InitialCatalog);

				var answer = char.ToUpper(Convert.ToChar(Console.Read()));
				while (answer != 'Y' && answer != 'N') {
					answer = char.ToUpper(Convert.ToChar(Console.Read()));
				}
				if (answer == 'N') {
					Console.WriteLine("create command cancelled.");
					return 1;
				}
				_overwrite = true;
			}

			var db = new Database() {Connection = _conn, Dir = _dir};
			try {
				db.CreateFromDir(_overwrite);
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