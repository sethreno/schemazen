using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Smo;
using NUnit.Framework;
using System.Resources;

namespace console
{
	class Program {
		static int Main(string[] args) {
            Options options = new Options(args);
            if (!options.Validate()) {
                foreach (string e in options.Errors) {
                    Console.WriteLine(e);
                }
                return -1;
            }

			Server srv = new Server(options.Server);
			Database db = default(Database);
			foreach (Database d in srv.Databases){
				if (d.Name == options.DB) {
					db = d;
					break;
				}
			}
			
			// create dir tree
            Console.WriteLine("creating directory tree");
			string[] dirs = { "data", "foreign_keys", "functions", 
                              "indexes", "procs", "tables", "triggers" };
			foreach (string dir in dirs) {
				if (!Directory.Exists(options.Dir + "/" + dir)) {
					Directory.CreateDirectory(options.Dir + "/" + dir);
				}
			}

			Scripter scr = new Scripter(srv);
			scr.Options.ScriptDrops = false;
			List<Urn> urns = new List<Urn>();

			// tables
            Console.WriteLine("scripting tables");
			foreach (Table t in db.Tables){
				if (t.IsSystemObject) continue;

				urns.Clear();
				urns.Add(t.Urn);
				scr.Options.WithDependencies = false;
				scr.Options.DriPrimaryKey = false;
				scr.Options.NoCollation = true;
				scr.Options.DriIndexes = false;
				scr.Options.DriDefaults = true;
				ScriptToFile(scr, urns.ToArray(), 
                    String.Format("{0}/tables/{1}.sql", options.Dir, t.Name));

				// foreign keys in seperate dir
				urns.Clear();
				foreach (ForeignKey fk in t.ForeignKeys) {
					urns.Add(fk.Urn);
				}
				if (urns.Count > 0) {
					scr.Options.DriAll = true;
					ScriptToFile(scr,urns.ToArray(), 
                        String.Format("{0}/foreign_keys/{1}.sql", options.Dir, t.Name));
				}

				// triggers in seperate dir
				urns.Clear();
				foreach (Trigger tr in t.Triggers) {
					urns.Add(tr.Urn);
				}
				if (urns.Count > 0) {
					scr.Options.DriAll = true;
					ScriptToFile(scr, urns.ToArray(), 
                        String.Format("{0}/triggers/{1}.sql", options.Dir, t.Name));
				}

				// indexes in seperate dir
				urns.Clear();
				foreach (Index idx in t.Indexes) {
					urns.Add(idx.Urn);
				}
				if (urns.Count > 0) {
                    scr.Options.DriAll = true;
					ScriptToFile(scr, urns.ToArray(), 
                        String.Format("{0}/indexes/{1}.sql", options.Dir, t.Name));
				}
			}

			// functions
            Console.WriteLine("scripting functions");
			foreach (UserDefinedFunction f in db.UserDefinedFunctions) {
				if (f.IsSystemObject) continue;
				urns.Clear();				
				urns.Add(f.Urn);
				scr.Options.DriAll = true;
				ScriptToFile(scr, urns.ToArray(), 
                    String.Format("{0}/functions/{1}.sql", options.Dir, f.Name));
			}			
			
			// procs
            Console.WriteLine("scripting stored procedures");
			foreach (StoredProcedure p in db.StoredProcedures) {
				if (p.IsSystemObject) continue;
				urns.Clear();
				urns.Add(p.Urn);
				scr.Options.DriAll = true;
				ScriptToFile(scr, urns.ToArray(), 
                    String.Format("{0}/procs/{1}.sql", options.Dir, p.Name));
			}

			// TODO data

            Console.WriteLine("success");
            return 0;
		}

		static void ScriptToFile(Scripter scr, Urn[] urns, string fileName) {
			using (TextWriter tw = File.CreateText(fileName)) {
				foreach (string line in scr.Script(urns)) {
					tw.WriteLine(line);
				}
				tw.WriteLine("GO");
				tw.Close();
			}
		}
	}

    [TestFixture()]
    class Options {
        public string Command;
        public string Server;
        public string DB;
        public string Dir;

        public Options() { }
        public Options(string[] args) {
            if (args.Length > 0) Command = args[0];
            if (args.Length > 1) Server = args[1];
            if (args.Length > 2) DB = args[2];
            if (args.Length > 3) Dir = args[3];

            if (string.IsNullOrEmpty(Dir)) Dir = ".";
        }

        public List<string> Errors = new List<string>();
        public bool Validate() {
            Errors.Clear();
            if (string.IsNullOrEmpty(Command)){
                Errors.Add("Command is required.");
            }
            if (string.IsNullOrEmpty(Server)) {
                Errors.Add("Server is required.");
            }
            if (string.IsNullOrEmpty(DB)) {
                Errors.Add("DB is required.");
            }
            return Errors.Count == 0;
        }

        [Test()]
        public void ValidateTest() {
            Options o = new Options();
            Assert.IsFalse(o.Validate());
            Assert.IsTrue(o.Errors.Contains("Command is required."));
            Assert.IsTrue(o.Errors.Contains("Server is required."));
            Assert.IsTrue(o.Errors.Contains("DB is required."));

            o.Command = "script";
            Assert.IsFalse(o.Validate());
            Assert.IsFalse(o.Errors.Contains("Command is required."));
            Assert.IsTrue(o.Errors.Contains("Server is required."));
            Assert.IsTrue(o.Errors.Contains("DB is required."));

            o.Server = "seth-pc\\sqlexpress";
            Assert.IsFalse(o.Validate());
            Assert.IsFalse(o.Errors.Contains("Command is required."));
            Assert.IsFalse(o.Errors.Contains("Server is required."));
            Assert.IsTrue(o.Errors.Contains("DB is required."));

            o.DB = "TESTDB";
            Assert.IsTrue(o.Validate());
            Assert.IsFalse(o.Errors.Contains("Command is required."));
            Assert.IsFalse(o.Errors.Contains("Server is required."));
            Assert.IsFalse(o.Errors.Contains("DB is required."));
        }
    }
}
