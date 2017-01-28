using System;
using System.Diagnostics;
using System.IO;

namespace SchemaZen.Library.Command {
	public class CreateCommand : BaseCommand {

		public void Execute(string databaseFilesPath) {
			var db = CreateDatabase();
			if (!Directory.Exists(db.Dir)) {
				throw new FileNotFoundException($"Snapshot dir {db.Dir} does not exist.");
			}

			if (!Overwrite && (DBHelper.DbExists(db.Connection))) {
				var msg = $"{Server} {DbName} already exists - use overwrite property if you want to drop it";
				throw new InvalidOperationException(msg);
			}

			db.CreateFromDir(Overwrite, databaseFilesPath, Logger.Log);
			Logger.Log(TraceLevel.Info, $"{Environment.NewLine}Database created successfully.");
		}
	}
}
