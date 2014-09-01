using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ManyConsole;
using model;
using NDesk.Options;

namespace console {
    internal class Dump : ConsoleCommand {
        private string _source;
        private string _target;

        private string[] _ignore;

        public Dump() {
            IsCommand("Dump", "Dumps a databases to xml.");
            Options = new OptionSet();
            SkipsCommandSummaryBeforeRunning();
            HasRequiredOption(
                "s|source=",
                "Connection string to a database to dump.",
                o => _source = o);
            HasRequiredOption(
                "t|target=",
                "File path where to save the dump.",
                o => _target = o);
            HasOption("i|ignore=",
                "Comma separated list of names to ignore. Works for tables and stored procedures.",
                o => _ignore = GetIgnoredNames(o));
        }

        public override int Run(string[] remainingArguments) {
            var sourceDb = new Database();
            sourceDb.Connection = _source;
            sourceDb.Load();

            sourceDb.Tables = sourceDb.Tables.Where(x => !_ignore.Contains(x.Name)).ToList();
            sourceDb.Routines = sourceDb.Routines.Where(x => !_ignore.Contains(x.Name)).ToList();

            var serializer = new XmlSerializer(typeof(Database));
            using (var stream = new StreamWriter(_target, false))
            {
                serializer.Serialize(stream, sourceDb);
            }

            return 0;
        }

        private static string[] GetIgnoredNames(string ignoreString) {
            var arr = ignoreString.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < arr.Length; i++) {
                arr[i] = arr[i].Trim();
            }

            return arr;
        }
    }
}