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

            if (!Directory.Exists(ScriptDir))
            {
                _logger.Log(TraceLevel.Error, string.Format("Snapshot dir {0} does not exist.", ScriptDir));
                return 1;
            }

            if (!Overwrite)
            {
                _logger.Log(TraceLevel.Verbose, "Checking if database already exists...");
                if (DBHelper.DbExists(ConnectionString))
                {
                    var question = string.Format("{0} {1} already exists - do you want to drop it",
                        Server, DbName);
                    if (!ConsoleQuestion.AskYN(question))
                    {
                        Console.WriteLine("Create command cancelled.");
                        return 1;
                    }
                    Overwrite = true;
                }
            }

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
		        createCommand.Execute(DatabaseFilesPath);
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
