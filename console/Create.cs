using System.Diagnostics;
using System.IO;
using SchemaZen.helpers;
using SchemaZen.model;

namespace SchemaZen.console {
	public class Create : DbCommand {
		public Create () : base("Create", "Create the specified database from scripts.") { }

		public override int Run(string[] remainingArguments) {
			var db = CreateDatabase();
			if (!Directory.Exists(db.Dir)) {
				Log(TraceLevel.Error, string.Format("Snapshot dir {0} does not exist.", db.Dir));
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

			try {
				db.CreateFromDir(Overwrite, Log);
				Log(TraceLevel.Info, string.Empty);
				Log(TraceLevel.Info, "Database created successfully.");
			} catch (BatchSqlFileException ex) {
				Log(TraceLevel.Info, string.Empty);
				Log(TraceLevel.Info, "Create completed with the following errors:");
				foreach (var e in ex.Exceptions)
				{
					Log(TraceLevel.Info, string.Format("- {0} (Line {1}):", e.FileName.Replace("/", "\\"), e.LineNumber));
					Log(TraceLevel.Error, string.Format(" {0}", e.Message));
				}

				return -1;
			} catch (SqlFileException ex) {
				Log(TraceLevel.Info, string.Empty);
				Log(TraceLevel.Info, string.Format(@"An unexpected SQL error occurred while executing scripts, and the process wasn't completed.
{0} (Line {1}):", ex.FileName.Replace("/", "\\"), ex.LineNumber));
				Log(TraceLevel.Error, ex.Message);
				return -1;
			}

			return 0;
		}
	}
}
