using System;
using System.Diagnostics;
using System.IO;

namespace SchemaZen.Library.Command {
    public class CreateCommand : BaseCommand {

        public void Execute(string databaseFilesPath)
        {
            var db = CreateDatabase();
            if (!Directory.Exists(db.Dir))
            {
                throw new FileNotFoundException(string.Format("Snapshot dir {0} does not exist.", db.Dir));
            }

            if (!Overwrite && !Merge && (DBHelper.DbExists(db.Connection)))
            {
                var msg = string.Format("{0} {1} already exists - use 'overwrite' property if you want to drop it or 'merge' property to merge the schema into an it.",
    Server, DbName);
                throw new InvalidOperationException(msg);
            }

            db.CreateFromDir(Overwrite, Merge, databaseFilesPath, Logger.Log);
            Logger.Log(TraceLevel.Info, Environment.NewLine + "Database created successfully.");
        }
    }
}