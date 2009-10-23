using System;
using System.IO;
using System.Data.SqlClient;
using CommandLine;
using CommandLine.Text;
using model;

namespace console {
	class Program {

		private enum Command {
			Script,
			Create
		}

		class Options {
			[Option("c", "command",
				Required = true,
				HelpText = @"Command to execute. Valid options include:
                           script - generate scripts for the specified db
                           create - create the specified db from scripts
            ")]
			public Command command;

			[Option("n", "conn_string",
				Required = true,
				HelpText = @"Connection string to the db to script or
                        create. The connection string can be read from the
                        <appSettings> or <connectionStrings> section of 
                        machine.config by prefixing the key name with as: or
                        cs: respectively.
              Examples:
                -n""server=localhost;database=DEVDB;Trusted_Connection=yes;"" 
                -nas:devcn - appSetting in machine.config named 'devcn'
                -ncs:devcn - connectionString in machine.config named 'devcn'
            ")]
			public string ConnString = "";

			[Option("s", "script_dir",
				Required = false,
				HelpText = @"Path to a schemanator script directory.
                        If omitted the current directory is used.                        
            ")]
			public string Dir = ".";

			[Option("d", "delete",
				Required = false,
				HelpText = @"Deletes existing db or script dir without promt.
                ")]
			public bool delete = false;

			[Option("v", "verbose",
				Required = false,
				HelpText = @"Print additional debug information to console.
                ")]
			public bool verbose = false;

			[Option(null, "data",
				Required = false,
				HelpText = @"A comma separated list of tables that contain
                        lookup data. The data from these tables will be 
                        exported to file when using the script command, and
                        imported into the database when using the create 
                        command. Regular expressions can be used to match 
                        multiple tables with the same naming pattern.
              Examples:
                --data ^lookup
                --data VehicleMake, VehicleModel
                --data ^lookup, VehicleMake, VehicleModel
            ")]
			public string data = "";

			[HelpOption(HelpText = "Display this help screen.")]
			public string GetHelp() {
				var txt = new HelpText("schemacon - Schemanator Console");
				txt.Copyright = new CopyrightInfo("Seth Reno", 2009);
				txt.AddPreOptionsLine(@"
Usage: schemacon [-dv] -c<command> -n<connection string> [-s<snapshot dir>]
                 [--data <tables>]
");
				txt.AddOptions(this);
				return txt;
			}
		}

		static int Main(string[] args) {
			var options = new Options();
			var parser = new CommandLineParser(new CommandLineParserSettings(Console.Out));
			if (!parser.ParseArguments(args, options)) {
				return -1;
			}

			if (options.ConnString.IndexOf("as:") == 0) {
				options.ConnString = ConfigHelper.GetAppSetting(options.ConnString.Substring(3));
			} else if (options.ConnString.IndexOf("cs:") == 0) {
				options.ConnString = ConfigHelper.GetConnectionString(options.ConnString.Substring(3));
			}

			switch (options.command) {
				case Command.Create:
					Create(options);
					break;

				case Command.Script:
					Script(options);
					break;
			}
			return 0;
		}

		private static void Create(Options options){
			if (String.IsNullOrEmpty(options.Dir)) {
				Console.WriteLine("You must specify a snapshot dir with the create command.");
				Environment.Exit(-1);
			}
			if (!Directory.Exists(options.Dir)) {
				Console.WriteLine("Snapshot dir {0} does not exist.", options.Dir);
				Environment.Exit(-1);
			}

			if (DBHelper.DbExists(options.ConnString) && !options.delete) {
				var cnBuilder = new SqlConnectionStringBuilder(options.ConnString);
				Console.Write("{0} {1} already exists do you want to drop it? (Y/N)",
				cnBuilder.DataSource, cnBuilder.InitialCatalog);

				var key = Console.ReadKey();
				if (key.Key != ConsoleKey.Y) {
					Environment.Exit(-1);
				}
				Console.WriteLine();
			}

			var db = new Database();
			db.Connection = options.ConnString;
			db.Dir = options.Dir;
			foreach (string pattern in options.data.Split(',')) {
				if (string.IsNullOrEmpty(pattern)) { continue; }
				foreach (Table t in db.FindTablesRegEx(pattern)) {
					db.DataTables.Add(t);
				}
			}
			db.CreateFromDir(options.delete);
		}

		private static void Script(Options options) {
			// load the model
			var db = new Database();
			db.Connection = options.ConnString;
			db.Dir = options.Dir;
			db.Load();

			// generate scripts
			if (!options.delete && Directory.Exists(options.Dir)) {
				Console.Write("{0} already exists do you want to replace it? (Y/N)", options.Dir);
				var key = Console.ReadKey();
				if (key.Key != ConsoleKey.Y) {
					Environment.Exit(-1);
				}
				Console.WriteLine();
			}

			foreach (string pattern in options.data.Split(',')) {
				if (string.IsNullOrEmpty(pattern)) { continue; }
				foreach (Table t in db.FindTablesRegEx(pattern)) {
					db.DataTables.Add(t);
				}
			}
			db.ScriptToDir(options.delete);

			Console.WriteLine("Snapshot successfully created at " + options.Dir);
		}
	}
}
