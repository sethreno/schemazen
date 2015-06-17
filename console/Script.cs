using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using model;

namespace console
{
	public class Script : DbCommand
	{

		protected string DataTables { get; set; }
		protected string DataTablesPattern { get; set; }

		public Script()
			: base(
				"Script", "Generate scripts for the specified database.")
		{
			this.HasOption(
				"dataTables=",
				"A comma separated list of tables to export data from.",
				o => this.DataTables = o);
			this.HasOption(
				"dataTablesPattern=",
				"A regular expression pattern that matches tables to export data from.",
				o => this.DataTablesPattern = o);
		}

		public override int Run(string[] args)
		{
			var db = this.CreateDatabase();
			db.Load();

			if (!string.IsNullOrEmpty(this.DataTables))
			{
				HandleDataTables(db, this.DataTables);
			}
			if (!string.IsNullOrEmpty(this.DataTablesPattern))
			{
				var tables = db.FindTablesRegEx(this.DataTablesPattern);
				foreach (var t in tables.Where(t => !db.DataTables.Contains(t)))
				{
					db.DataTables.Add(t);
				}
			}

			if (!this.Overwrite && Directory.Exists(db.Dir))
			{
				Console.Write("{0} already exists do you want to replace it? (Y/N)", db.Dir);
				var key = Console.ReadKey();
				if (key.Key != ConsoleKey.Y)
				{
					return 1;
				}
				Console.WriteLine();
			}

			db.ScriptToDir(this.Overwrite);

			Console.WriteLine("Snapshot successfully created at " + db.Dir);
			return 0;
		}

		private static void HandleDataTables(Database db, string tableNames)
		{
			foreach (var value in tableNames.Split(','))
			{
				var schema = "dbo";
				var name = value;
				if (value.Contains("."))
				{
					schema = value.Split('.')[0];
					name = value.Split('.')[1];
				}
				var t = db.FindTable(name, schema);
				if (t == null)
				{
					Console.WriteLine(
						"warning: could not find data table {0}.{1}",
						schema, name);
				}
				if (db.DataTables.Contains(t)) continue;
				db.DataTables.Add(t);
			}
		}
	}
}