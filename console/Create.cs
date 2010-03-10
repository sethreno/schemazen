using model;
using System;
using System.IO;
using System.Data.SqlClient;

namespace console {
    public class Create : ICommand {

        private Operand source;
        private Operand destination;
        private DataArg data = null;
        private bool delete = false;

        public bool Parse(string[] args) {
            if (args.Length < 3) { return false; }
            if (!args[1].ToLower().StartsWith("dir:")) {
                args[1] = "dir:" + args[1];
            }
            source = Operand.Parse(args[1]);       
            destination = Operand.Parse(args[2]);
            foreach (string arg in args) {
                if (arg.ToLower() == "-d") delete = true;
            }

            if (source == null || destination == null) return false;
            if (source.OpType != OpType.ScriptDir) return false;
            if (destination.OpType != OpType.Database) return false;

            return true;
        }
        public string GetUsageText() {
            return @"create <source> <destination> [--data <tables>] [-d]

Create the specified database from script

<source>                Path to the directory where scripts are located.

<destination>           The connection string to the database to create.
                        Must be prefixed with a type identifer. Valid type
                        identifiers include: cn:, cs:, as:
                        cn: - connection string
                        cs: - <conectionString> from machine.config
                        as: - <appSetting> from machine.config                        
              Examples:
                cn:""server=localhost;database=DEVDB;Trusted_Connection=yes;""
                cs:devcn - connectionString in machine.config named 'devcn'
                as:devcn - appSetting in machine.config named 'devcn'

--data                  A comma separated list of tables that contain
                        lookup data. The data for these tables will be 
                        imported from the corresponding text files in the
                        data directory. Regular expressions can be used to
                        match multiple tables with the same naming pattern.
              Examples:
                --data ^lookup
                --data VehicleMake, VehicleModel
                --data ^lookup, VehicleMake, VehicleModel

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
                Console.Write("{0} {1} already exists do you want to drop it? (Y/N)",
                cnBuilder.DataSource, cnBuilder.InitialCatalog);

                var key = Console.ReadKey();
                if (key.Key != ConsoleKey.Y) {
                    return false;
                }
                Console.WriteLine();
            }

            var db = new Database();
            db.Connection = destination.Value;
            db.Dir = source.Value;
            if (data != null) {
                foreach (string pattern in data.Value.Split(',')) {
                    if (string.IsNullOrEmpty(pattern)) { continue; }
                    foreach (Table t in db.FindTablesRegEx(pattern)) {
                        db.DataTables.Add(t);
                    }
                }
            }
            
            db.CreateFromDir(delete);
            Console.WriteLine("Database created successfully.");
            return true;
        }
    }
}
