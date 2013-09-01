using System;
using System.Data.SqlClient;
using System.IO;
using model;

namespace console {
	public class Create : ICommand {
		private DataArg data;
		private bool delete;
		private Operand destination;
		private Operand source;

		public bool Parse(string[] args) {
			if (args.Length < 3) {
				return false;
			}
			source = Operand.Parse(args[1]);
			destination = Operand.Parse(args[2]);
			data = DataArg.Parse(args);
			foreach (string arg in args) {
				if (arg.ToLower() == "-d") delete = true;
			}

			if (source == null || destination == null) return false;
			if (source.OpType != OpType.ScriptDir) return false;
			if (destination.OpType != OpType.Database) return false;

			return true;
		}

		public string GetUsageText() {
			return @"create <source> <destination> [-d]

Create the specified database from script

<source>                Path to the directory where scripts are located.

<destination>           The connection string to the database to create.
              Example:
                ""server=localhost;database=DEVDB;Trusted_Connection=yes;""

-d                      Delete existing database without prompt.
";
		}

		public bool Run() {
			if (String.IsNullOrEmpty(source.Value)) {
				Console.WriteLine("You must specify a snapshot dir with the create command.");
				return false;
			}
			if (!Directory.Exists(source.Value)) {
				Console.WriteLine("Snapshot dir {0} does not exist.", source.Value);
				return false;
			}

			if (DBHelper.DbExists(destination.Value) && !delete) {
				var cnBuilder = new SqlConnectionStringBuilder(destination.Value);
				Console.WriteLine("{0} {1} already exists do you want to drop it? (Y/N)",
					cnBuilder.DataSource, cnBuilder.InitialCatalog);

				char answer = char.ToUpper(Convert.ToChar(Console.Read()));
				while (answer != 'Y' && answer != 'N') {
					answer = char.ToUpper(Convert.ToChar(Console.Read()));
				}
				if (answer == 'N') {
					Console.WriteLine("create command cancelled.");
					return false;
				}
				delete = true;
			}

			var db = new Database();
			db.Connection = destination.Value;
			db.Dir = source.Value;
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

			try {
				db.CreateFromDir(delete);
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
				return false;
			}
			catch (DataFileException ex) {
				Console.Write(@"A SQL error occurred while loading data.
{0}(Line {1}): {2}", ex.FileName.Replace("/", "\\"), ex.LineNumber, ex.Message);
				return false;
			}

			return true;
		}
	}
}