using ManyConsole;
using Mono.Options;
using System;

namespace SchemaZen.Console;

public abstract class BaseCommand : ConsoleCommand {
	protected BaseCommand(string command, string oneLineDescription) {
		IsCommand(command, oneLineDescription);
		Options = new OptionSet();
		SkipsCommandSummaryBeforeRunning();

		HasOption("s|server=", "server", o => Server = o);
		HasOption("b|database=", "database", o => DbName = o);
		HasOption("c|connectionString=", "connection string", o => ConnectionString = o);
		HasOption("u|user=", "user", o => User = o);
		HasOption("p|pass=", "pass", o => Pass = o);
		HasRequiredOption(
			"d|scriptDir=",
			"Path to database script directory.",
			o => ScriptDir = o);
		HasOption(
			"o|overwrite",
			"Overwrite existing target without prompt.",
			o => Overwrite = o != null);
		HasOption(
			"v|verbose",
			"Enable verbose log messages.",
			o => Verbose = o != null);
		HasOption(
			"f|databaseFilesPath=",
			"Path to database data and log files.",
			o => DatabaseFilesPath = o);
		HasOption(
			"timeout=",
			"Number of seconds for sql command timeout (default 30).",
				o => TimeoutSec = (o == null ? 30 : Convert.ToInt32(o))
		);
	}

	protected string Server { get; set; }
	protected string DbName { get; set; }
	protected string ConnectionString { get; set; }
	protected string User { get; set; }
	protected string Pass { get; set; }
	protected string ScriptDir { get; set; }
	protected bool Overwrite { get; set; }
	protected bool Verbose { get; set; }
	protected string DatabaseFilesPath { get; set; }
	protected int TimeoutSec { get; set; }
}
