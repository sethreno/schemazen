using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SchemaZen.Library.Models;

namespace SchemaZen.Library.Command {
    public class CreateCommand : BaseCommand {

        public void Execute(string databaseFilesPath)
        {
            var db = CreateDatabase();
            if (!Directory.Exists(db.Dir))
            {
                throw new FileNotFoundException(string.Format("Snapshot dir {0} does not exist.", db.Dir));
            }

            if (!Overwrite && (DBHelper.DbExists(db.Connection)))
            {
                var msg = string.Format("{0} {1} already exists - use overwrite property if you want to drop it",
    Server, DbName);
                throw new InvalidOperationException(msg);
            }

            db.CreateFromDir(Overwrite, databaseFilesPath, Logger.Log);
            Logger.Log(TraceLevel.Info, Environment.NewLine + "Database created successfully.");
        }
    }
}