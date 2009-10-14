using System;
using System.IO;
using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;
using model;
using System.Data.SqlClient;

namespace console
{
	class Program {

        private enum Command{
            Script,
            Create
        }

        class Options
        {
            [Option("c", "command",
                Required = true,
                HelpText = @"Command to execute. Valid options include:
                           script - generate scripts for the specified db
                           create - create the specified db from scripts
            ")]
            public Command command;

            [Option("n", "conn_string",
                Required = true,                
                HelpText = @"Connection string to the db or a .net app
                        setting containing the connection string.
            ")]
            public string ConnString = "";

            [Option("s", "snapshot_dir",
                Required = false,
                HelpText = @"Path to a schemanator snapshot directory.
                        Required for the 'create' command. If the 'script'
                        command is used without it all scripts will be combined
                        and written to standard out.                        
            ")]
            public string Dir = "";

            [Option("d","delete",
                Required =false,
                HelpText = @"Deletes existing database or snapshot without promt.
                ")]
            public bool delete = false;

            [Option("v","verbose",
                Required = false,
                HelpText = @"Print additional debug information to console.
                ")]
            public bool verbose = false;

            [HelpOption(HelpText = "Display this help screen.")]
            public string GetHelp(){
                var txt = new HelpText("schemacon - Schemanator Console");
                txt.Copyright = new CopyrightInfo("Seth Reno", 2009);                
                txt.AddPreOptionsLine("\nUsage: schemacon [-dv] -c<command> -n<connection string> [-s<snapshot dir>]");
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
            
            if (!options.ConnString.Contains(";")) {
                var appSettings = new System.Configuration.AppSettingsReader();
                options.ConnString = (string)appSettings.GetValue(options.ConnString, typeof(string));
            }

            switch (options.command){
                case Command.Create:
                    if (String.IsNullOrEmpty(options.Dir)) {
                        Console.WriteLine("You must specify a snapshot dir with the create command.");
                        Environment.Exit(-1);
                    }
                    if (!Directory.Exists(options.Dir)) {
                        Console.WriteLine("Snapshot dir {0} does not exist.", options.Dir);
                        Environment.Exit(-1);
                    }
                    
                    CreateDb(options);
                    break;

                case Command.Script:
                   // load the model
                   var db = new Database();
                   db.Load(options.ConnString);

                   // generate scripts
                   if (!String.IsNullOrEmpty(options.Dir))                   {
                       ScriptToDir(options, db);
                   } else {
                       ScriptToOutput(options, db);
                   }
                   break;
            }                    
                       
            return 0;
		}

        private static string[] dirs = { "tables",    "foreign_keys",  "data",
                                         "functions", "procs",         "triggers" };

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
            if (Directory.Exists(options.Dir)){
                if (!options.delete) {
                    Console.Write("{0} already exists do you want to replace it? (Y/N)", options.Dir);
                    var key = Console.ReadKey();
                    if (key.Key != ConsoleKey.Y) {
                        Environment.Exit(-1);
                    }
                    Console.WriteLine();
                }
                
                // delete the existing script files
                foreach (string dir in dirs) {
                    if (!Directory.Exists(options.Dir +"/" +dir)) break;
                    foreach (string f in Directory.GetFiles(options.Dir + "/" + dir)){
                        File.Delete(f);
                    }
                }
            }
            // create dir tree
            Console.WriteLine("creating directory tree");            
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

            Console.WriteLine("Snapshot successfully created at " + options.Dir);
        }

        private static void CreateDb(Options options) {
            DBHelper.EchoSql = options.verbose;
            var cnBuilder = new SqlConnectionStringBuilder(options.ConnString);
            if (DBHelper.DbExists(options.ConnString)) {
                if (!options.delete) {                    
                    Console.Write(
                        "{0} {1} already exists do you want to drop it? (Y/N)",
                        cnBuilder.DataSource, cnBuilder.InitialCatalog);
                    var key = Console.ReadKey();                    
                    if (key.Key != ConsoleKey.Y) {
                        Environment.Exit(-1);
                    }
                    Console.WriteLine();                   
                }
                DBHelper.DropDb(options.ConnString);
            }

            //create database
            DBHelper.CreateDb(options.ConnString);

            //run scripts
            foreach (string dir in dirs) {
                if ("data" == dir) { continue; }
                Console.WriteLine("creating {0}", dir);
                foreach (string f in Directory.GetFiles(options.Dir + "/" + dir)) {
                    DBHelper.ExecBatchSql(options.ConnString, File.ReadAllText(f));
                }
            }

            Console.WriteLine("{0} {1} successfully created.",
                cnBuilder.DataSource, cnBuilder.InitialCatalog);
        }
	}
}
