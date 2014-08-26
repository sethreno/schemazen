using System.IO;
using System.Xml.Serialization;
using ManyConsole;
using model;
using NDesk.Options;

namespace console {
    internal class Dump : ConsoleCommand {
        private string _source;
        private string _target;

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
        }

        public override int Run(string[] remainingArguments) {
            var sourceDb = new Database();
            sourceDb.Connection = _source;
            sourceDb.Load();

            var serializer = new XmlSerializer(typeof(Database));
            using (var stream = new StreamWriter(_target, false))
            {
                serializer.Serialize(stream, sourceDb);
            }

            return 0;
        }
    }
}