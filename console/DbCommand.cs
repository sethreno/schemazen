﻿using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using ManyConsole;
using NDesk.Options;
using SchemaZen.model;

namespace SchemaZen.console {
	public abstract class DbCommand : ConsoleCommand {
		protected DbCommand(string command, string oneLineDescription) {
			IsCommand(command, oneLineDescription);
			Options = new OptionSet();
			SkipsCommandSummaryBeforeRunning();
			HasRequiredOption("s|server=", "server", o => Server = o);
			HasRequiredOption("b|database=", "database", o => DbName = o);
			HasOption("u|user=", "user", o => User = o);
			HasOption("p|pass=", "pass", o => Pass = o);
			HasRequiredOption(
				"d|scriptDir=",
				"Path to database script directory.",
				o => ScriptDir = o);
			HasOption(
				"o|overwrite=",
				"Overwrite existing target without prompt.",
				o => Overwrite = o != null);
			HasOption("v|verbose=",
				"Verbose output of actions.",
				o => Verbose = o != null);
		}

		protected string Server { get; set; }
		protected string DbName { get; set; }
		protected string User { get; set; }
		protected string Pass { get; set; }
		protected string ScriptDir { get; set; }
		protected bool Overwrite { get; set; }
		protected bool Verbose { get; set; }

		protected Database CreateDatabase() {
			var builder = new SqlConnectionStringBuilder {
				DataSource = Server,
				InitialCatalog = DbName,
				IntegratedSecurity = string.IsNullOrEmpty(User),
				ApplicationName = ((AssemblyTitleAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyTitleAttribute), false)).Title
			};
			if (!builder.IntegratedSecurity) {
				builder.UserID = User;
				builder.Password = Pass;
			}
			return new Database {
				Connection = builder.ToString(),
				Dir = ScriptDir
			};
		}

		protected void Log(TraceLevel level, string message)
		{
			var prevColor = Console.ForegroundColor;

			switch (level) {
				case TraceLevel.Error:
					Console.ForegroundColor = ConsoleColor.DarkRed;
					break;
				case TraceLevel.Verbose:
					if (!Verbose)
						return;
					break;
				case TraceLevel.Warning:
					//Console.ForegroundColor = ConsoleColor.Red;
					break;
			}

			if (message.EndsWith("\r"))
				Console.Write(message);
			else
				Console.WriteLine(message);

			Console.ForegroundColor = prevColor;
		}
	}
}
