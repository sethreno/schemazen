using System;
using System.Diagnostics;
using System.IO;
using model;

namespace console
{
	public class Create : DbCommand
	{

		public Create()
			: base(
				"Create", "Create the specified database from scripts.") { }

		public override int Run(string[] remainingArguments)
		{
			var db = this.CreateDatabase();
			if (!Directory.Exists(db.Dir))
			{
				Console.WriteLine("Snapshot dir {0} does not exist.", db.Dir);
				return 1;
			}

			if (DBHelper.DbExists(db.Connection) && !this.Overwrite)
			{
				Console.Write("{0} {1} already exists do you want to drop it (Y/N)? ", this.Server, this.DbName);
				char answer = ' ';
				while (answer != 'Y' && answer != 'N')
				{
					answer = char.ToUpper(Convert.ToChar(Console.Read()));
				}
				if (answer == 'N')
				{
					Console.WriteLine("Create command cancelled.");
					return 1;
				}
				this.Overwrite = true;
			}

			try
			{
				db.CreateFromDir(this.Overwrite);
				Console.WriteLine("Database created successfully.");
			}
			catch (BatchSqlFileException ex)
			{
				Console.WriteLine(@"Create completed with the following errors:");
				foreach (var e in ex.Exceptions)
				{
					Console.WriteLine(@"{0}(Line {1}): {2}", e.FileName.Replace("/", "\\"), e.LineNumber, e.Message);
				}
				return -1;
			}
			catch (SqlFileException ex)
			{
				Console.Write(@"A SQL error occurred while executing scripts.
{0} (Line {1}): {2}", ex.FileName.Replace("/", "\\"), ex.LineNumber, ex.Message);
				return -1;
			}

			return 0;
		}
	}
}