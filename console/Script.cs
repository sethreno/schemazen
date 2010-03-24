using System;
using model;
using System.IO;

namespace console {
    public class Script : ICommand {
        public bool Parse(string[] args) {
            if (args.Length < 3) { return false; }
            source = Operand.Parse(args[1]);
            if (!args[2].ToLower().StartsWith("dir:")) {
                args[2] = "dir:" + args[2];
            }
            destination = Operand.Parse(args[2]);
            data = DataArg.Parse(args);
            foreach (string arg in args) {
                if (arg.ToLower() == "-d") delete = true;                
            }           

            if (source == null || destination == null) return false;            
            if (source.OpType != OpType.Database)return false;
            if (destination.OpType != OpType.ScriptDir) return false;            
            
            return true;
        }
        public string GetUsageText() {
            return @"script <source> <destination> [--data <tables>] [-d]

Generate scripts for the specified database.

<source>                The connection string to the database to script.
                        Must be prefixed with a type identifer. Valid type
                        identifiers include: cn:, cs:, as:
                        cn: - connection string
                        cs: - <conectionString> from machine.config
                        as: - <appSetting> from machine.config                        
              Examples:
                cn:""server=localhost;database=DEVDB;Trusted_Connection=yes;""
                cs:devcn - connectionString in machine.config named 'devcn'
                as:devcn - appSetting in machine.config named 'devcn'

<destination>           Path to the directory where scripts will be created                

--data                  A comma separated list of tables that contain
                        lookup data. The data from these tables will be 
                        exported to text files. Regular expressions can 
                        be used to match multiple tables with the same 
                        naming pattern.
              Examples:
                --data ^lookup
                --data VehicleMake, VehicleModel
                --data ^lookup, VehicleMake, VehicleModel

-d                      Delete existing scripts without prompt.
";
        }

        private Operand source;
        private Operand destination;
        private DataArg data = null;
        private bool delete = false;

        public bool Run() {
            // load the model
            var db = new Database();
            db.Connection = source.Value;
            db.Dir = destination.Value;
            db.Load();

            // generate scripts
            if (!delete && Directory.Exists(destination.Value)) {
                Console.Write("{0} already exists do you want to replace it? (Y/N)", destination.Value);
                var key = Console.ReadKey();
                if (key.Key != ConsoleKey.Y) {
                    return false;
                }
                Console.WriteLine();
            }

            if (data != null) {
                foreach (string pattern in data.Value.Split(',')) {
                    if (string.IsNullOrEmpty(pattern)) { continue; }
                    foreach (Table t in db.FindTablesRegEx(pattern)) {
                        db.DataTables.Add(t);
                    }
                }
            }
            
            db.ScriptToDir(delete);

            Console.WriteLine("Snapshot successfully created at " + destination.Value);
            return true;
        }
    }
}
