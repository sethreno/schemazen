using System;
using System.IO;
using System.Collections.Generic;

using model;

namespace console
{
	class Program {
		static int Main(string[] args) {
            Options options = new Options(args);
            if (!options.Validate()) {
                foreach (string e in options.Errors) {
                    Console.WriteLine(e);
                }
                return -1;
            }
			
			// create dir tree
            Console.WriteLine("creating directory tree");
			string[] dirs = { "data",  "foreign_keys", "functions", 
                              "procs", "tables",       "triggers" };
			foreach (string dir in dirs) {
				if (!Directory.Exists(options.Dir + "/" + dir)) {
					Directory.CreateDirectory(options.Dir + "/" + dir);
				}
			}

            // load the model
            var db = new Database();
            db.Load(options.ConnString);
                        
            Console.WriteLine("scripting tables");
			foreach (Table t in db.Tables){
                File.WriteAllText(
                    String.Format("{0}/tables/{1}.sql", options.Dir, t.Name),
                    t.ScriptCreate() + "\r\nGO\r\n"
                );
			}
                        
            Console.WriteLine("scripting foreign keys");
            foreach (ForeignKey fk in db.ForeignKeys){           
                File.AppendAllText(
                    String.Format("{0}/foreign_keys/{1}.sql", options.Dir, fk.Table.Name),
                    fk.ScriptCreate() + "\r\nGO\r\n"
                );
            }
            			
            Console.WriteLine("scripting procs, functions, & triggers");
            foreach (Routine r in db.Routines)
            {
                string dir = "procs";
                if (r.Type == "TRIGGER") { dir = "triggers"; }
                if (r.Type == "FUNCTION"){ dir = "functions"; }
                File.WriteAllText(
                    String.Format("{0}/{1}/{2}.sql", options.Dir, dir, r.Name),
                    r.ScriptCreate() + "\r\nGO\r\n"
                );
            }

			// TODO data

            Console.WriteLine("success");
            return 0;
		}
	}
        
    class Options {
        public string ConnString;
        public string Dir;

        public Options() { }
        public Options(string[] args) {
            if (args.Length > 0) ConnString = args[0];
            if (args.Length > 1) Dir = args[1];

            if (string.IsNullOrEmpty(Dir)) Dir = ".";
        }

        public List<string> Errors = new List<string>();
        public bool Validate() {
            Errors.Clear();
            if (string.IsNullOrEmpty(ConnString)){
                Errors.Add("ConnString is required.");
            }
            return Errors.Count == 0;
        }
    }
}
