using System;
using System.Diagnostics;
using System.IO;
using ManyConsole;
using SchemaZen.Library;
using SchemaZen.Library.Command;
using SchemaZen.Library.Models;

namespace SchemaZen.console {
	public class Create : BaseCommand {
		private Logger _logger;
		public Create()
			: base(
				"Create", "Create the specified database from scripts.") { }

		public override int Run(string[] remainingArguments) {
			_logger = new Logger(Verbose);

			var createCommand = new CreateCommand {
				ConnectionString = ConnectionString,
				DbName = DbName,
				Pass = Pass,
				ScriptDir = ScriptDir,
				Server = Server,
				User = User,
				Logger = _logger,
				Overwrite = Overwrite
			};

			try {
				createCommand.Execute(DatabaseFilesPath);
			} catch (BatchSqlFileException ex) {
				_logger.Log(TraceLevel.Info, $"{Environment.NewLine}Create completed with the following errors:");
				foreach (var e in ex.Exceptions) {
					_logger.Log(TraceLevel.Info, $"- {e.FileName.Replace("/", "\\")} (Line {e.LineNumber}):");
					_logger.Log(TraceLevel.Error, $" {e.Message}");
				}
				return -1;
			} catch (SqlFileException ex) {
				_logger.Log(TraceLevel.Info, $@"{Environment.NewLine}An unexpected SQL error occurred while executing scripts, and the process wasn't completed.
{ex.FileName.Replace("/", "\\")} (Line {ex.LineNumber}):");
				_logger.Log(TraceLevel.Error, ex.Message);
				return -1;
			} catch (Exception ex) {
				throw new ConsoleHelpAsException(ex.Message);
			}
			return 0;
		}
	}
}
