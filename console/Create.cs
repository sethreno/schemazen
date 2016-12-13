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

            var createCommand = new CreateCommand
            {
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
                createCommand.CreateDatabase(DatabaseFilesPath);
                createCommand.CreateTables(DatabaseFilesPath);
		    } catch (BatchSqlFileException ex) {
		        _logger.Log(TraceLevel.Info, Environment.NewLine + "Create completed with the following errors:");
		        foreach (var e in ex.Exceptions) {
		            _logger.Log(TraceLevel.Info,
		                string.Format("- {0} (Line {1}):", e.FileName.Replace("/", "\\"), e.LineNumber));
		            _logger.Log(TraceLevel.Error, string.Format(" {0}", e.Message));
		        }
		        return -1;
		    } catch (SqlFileException ex) {
		        _logger.Log(TraceLevel.Info,
		            Environment.NewLine +
		            string.Format(@"An unexpected SQL error occurred while executing scripts, and the process wasn't completed.
{0} (Line {1}):", ex.FileName.Replace("/", "\\"), ex.LineNumber));
		        _logger.Log(TraceLevel.Error, ex.Message);
		        return -1;
		    } catch (Exception ex) {
		        throw new ConsoleHelpAsException(ex.Message);
		    }
		    return 0;
		}
	}
}
