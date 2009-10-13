using System;
using System.IO;
using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;
using model;

namespace console
{
	class Program {
        class Options
        {
            [Option("c", "conn_string",
                Required = false,                
                HelpText = @"Connection string to the db to script.

      Examples: ""Server=localhost;Database=MYDB;Trusted_Connection=True;""
                ""Server=localhost;Database=MYDB;User Id=user;Password=pass;""
            ")]
            public string ConnString = "";

            [Option("a", "conn_appSetting",
                Required = false,
                HelpText = @"The key for a .net appSetting that contains
                        the connection string to the db to script.
            ")]
            public string ConnAppSetting = "";

            [Option("d", "output_dir",
                Required = false,
                HelpText = @"Output directory for generated scripts.
                        If this option is used a separate script will be
                        generated for each object. Scripts are grouped
                        into sub directories by object type.
            ")]
            public string Dir = "";

            [HelpOption(HelpText = "Display this help screen.")]
            public string GetHelp(){
                var txt = new HelpText("schemacon - schemanator console");
                txt.Copyright = new CopyrightInfo("Seth Reno", 2009);
                txt.AddPreOptionsLine(@"
Usage: schemacon -c <connection string> [-d <output dir>]
       schemacon -a <app setting> [-d <output dir>]");
                txt.AddOptions(this);
                return txt;
            }
        }

		static int Main(string[] args) {
            var options = new Options();
            var parser = new CommandLineParser(new CommandLineParserSettings(Console.Error));
            if (!parser.ParseArguments(args, options)){
                return -1;
            }
            if (string.IsNullOrEmpty(options.ConnAppSetting)
             && string.IsNullOrEmpty(options.ConnString)){
                 Console.WriteLine("You must specify a connection string or app setting.");
                 Console.WriteLine("Enter \"schemacon --help\" for more info.");
                 return -1;
            }

            if (!String.IsNullOrEmpty(options.ConnAppSetting)) {
                var appSettings = new System.Configuration.AppSettingsReader();
                options.ConnString = (string)appSettings.GetValue(options.ConnAppSetting, typeof(string));
            }

            // load the model
            var db = new Database();
            db.Load(options.ConnString);

            // generate scripts
            if (!String.IsNullOrEmpty(options.Dir)) {
                ScriptToDir(options, db);
            } else {
                ScriptToOutput(options, db);
            }
                       
            return 0;
		}

        private static void ScriptToOutput(Options options, Database db) {
            foreach (Table t in db.Tables) {
                Console.WriteLine(t.ScriptCreate());
                Console.WriteLine("GO");                
            }
            foreach (ForeignKey fk in db.ForeignKeys) {
                Console.WriteLine(fk.ScriptCreate());
                Console.WriteLine("GO");
            }
            foreach (Routine r in db.Routines) {
                Console.WriteLine(r.ScriptCreate());
                Console.WriteLine("GO");
            }
        }

        private static void ScriptToDir(Options options, Database db) {
            // create dir tree
            Console.WriteLine("creating directory tree");
            string[] dirs = { "data",  "foreign_keys", "functions", 
                              "procs", "tables",       "triggers" };
            foreach (string dir in dirs) {
                if (!Directory.Exists(options.Dir + "/" + dir)) {
                    Directory.CreateDirectory(options.Dir + "/" + dir);
                }
            }

            Console.WriteLine("scripting tables");
            foreach (Table t in db.Tables) {
                File.WriteAllText(
                    String.Format("{0}/tables/{1}.sql", options.Dir, t.Name),
                    t.ScriptCreate() + "\r\nGO\r\n"
                );
            }

            Console.WriteLine("scripting foreign keys");
            foreach (ForeignKey fk in db.ForeignKeys) {
                File.AppendAllText(
                    String.Format("{0}/foreign_keys/{1}.sql", options.Dir, fk.Table.Name),
                    fk.ScriptCreate() + "\r\nGO\r\n"
                );
            }

            Console.WriteLine("scripting procs, functions, & triggers");
            foreach (Routine r in db.Routines) {
                string dir = "procs";
                if (r.Type == "TRIGGER") { dir = "triggers"; }
                if (r.Type == "FUNCTION") { dir = "functions"; }
                File.WriteAllText(
                    String.Format("{0}/{1}/{2}.sql", options.Dir, dir, r.Name),
                    r.ScriptCreate() + "\r\nGO\r\n"
                );
            }

            Console.WriteLine("success");
        }
	}
}
