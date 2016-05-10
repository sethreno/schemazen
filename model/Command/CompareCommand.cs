using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SchemaZen.Library.Models;

namespace SchemaZen.Library.Command {
        public string Source;
        public string Target;
        public bool Verbose;
        public string OutDiff;

        public bool Execute()
        {
            var sourceDb = new Database();
            var targetDb = new Database();
            sourceDb.Connection = Source;
            targetDb.Connection = Target;
            sourceDb.Load();
            targetDb.Load();
            var diff = sourceDb.Compare(targetDb);
            if (diff.IsDiff)
            {
                Console.WriteLine("Databases are different.");
                Console.WriteLine(diff.SummarizeChanges(Verbose));
                if (!string.IsNullOrEmpty(OutDiff))
                {
                    Console.WriteLine();
                    if (!Overwrite && File.Exists(OutDiff))
                    {
                        var message = string.Format(
                            "{0} already exists - set overwrite to true if you want to delete it", OutDiff);
                        throw new InvalidOperationException(message);
                    }
                    File.WriteAllText(OutDiff, diff.Script());
                    Console.WriteLine("Script to make the databases identical has been created at {0}",
                        Path.GetFullPath(OutDiff));
                }
                return true;
            }
            Console.WriteLine("Databases are identical.");
            return false;
        }
    }
}
