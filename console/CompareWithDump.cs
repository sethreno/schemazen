using System;
using System.IO;
using System.Xml.Serialization;
using ManyConsole;
using model;
using NDesk.Options;

namespace console {
    internal class CompareWithDump : ConsoleCommand {
        private string _source;
        private string _target;

        public CompareWithDump() {
            IsCommand("CompareWithDump", "Compares a database with a xml dump.");
            Options = new OptionSet();
            SkipsCommandSummaryBeforeRunning();
            HasRequiredOption(
                "s|source=",
                "Connection string to a database to compare.",
                o => _source = o);
            HasRequiredOption(
                "t|target=",
                "Path to an xml dump to compare.",
                o => _target = o);
        }

        public override int Run(string[] remainingArguments) {
            var sourceDb = new Database();
            sourceDb.Connection = _source;
            sourceDb.Load();

            Database targetDb;
            var serializer = new XmlSerializer(typeof(Database));
            using (var stream = new StreamReader(_target, false)) {
                targetDb = (Database) serializer.Deserialize(stream);
            }

            DatabaseDiff diff = sourceDb.Compare(targetDb, new DefaultCompareCompareConfig());

            var diffSerializer = new XmlSerializer(typeof(DatabaseDiff));
            using (var stream = new StreamWriter("diff.xml", false))
            {
                diffSerializer.Serialize(stream, diff);
            }

            if (diff.IsDiff)
            {
                Console.WriteLine("Databases are different.");
                return 1;
            }
            Console.WriteLine("Databases are identical.");
            return 0;
        }
    }
}