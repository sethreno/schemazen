using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Smo;

namespace console
{
	class Program
	{
		static void Main(string[] args)
		{
			Server srv = new Server(args[0]);
			Database db = default(Database);
			foreach (Database d in srv.Databases){
				if (d.Name == args[1]) {
					db = d;
					break;
				}
			}
			
			// create dir tree
			string[] dirs = { "data", "foreign_keys", "functions", "indexes", "procs", "tables", "triggers" };						
			foreach (string dir in dirs) {
				if (!Directory.Exists(args[2] + "/" + dir)) {
					Directory.CreateDirectory(args[2] + "/" + dir);
				}
			}

			Scripter scr = new Scripter(srv);
			scr.Options.ScriptDrops = false;
			List<Urn> urns = new List<Urn>();

			// tables		
			foreach (Table t in db.Tables){
				if (t.IsSystemObject) continue;

				urns.Clear();
				urns.Add(t.Urn);
				scr.Options.WithDependencies = false;
				scr.Options.DriPrimaryKey = true;
				scr.Options.NoCollation = true;
				scr.Options.DriIndexes = false;
				scr.Options.DriDefaults = true;
				ScriptToFile(scr, urns.ToArray(), String.Format("{0}/tables/{1}.sql", args[2], t.Name));

				// foreign keys in seperate dir				
				urns.Clear();
				foreach (ForeignKey fk in t.ForeignKeys) {					
					urns.Add(fk.Urn);					
				}
				if (urns.Count > 0) {
					scr.Options.DriAll = true;					
					ScriptToFile(scr,urns.ToArray(), String.Format("{0}/foreign_keys/{1}.sql", args[2], t.Name));
				}

				// triggers in seperate dir
				urns.Clear();
				foreach (Trigger tr in t.Triggers) {
					urns.Add(tr.Urn);
				}
				if (urns.Count > 0) {
					scr.Options.DriAll = true;
					ScriptToFile(scr, urns.ToArray(), String.Format("{0}/triggers/{1}.sql", args[2], t.Name));
				}

				// indexes in seperate dir
				urns.Clear();
				foreach (Index idx in t.Indexes) {
					urns.Add(idx.Urn);
				}
				if (urns.Count > 0) {
					scr.Options.DriAll = true;
					ScriptToFile(scr, urns.ToArray(), String.Format("{0}/indexes/{1}.sql", args[2], t.Name));
				}
			}

			// functions			
			foreach (UserDefinedFunction f in db.UserDefinedFunctions) {
				if (f.IsSystemObject) continue;
				urns.Clear();				
				urns.Add(f.Urn);
				scr.Options.DriAll = true;
				ScriptToFile(scr, urns.ToArray(), String.Format("{0}/functions/{1}.sql", args[2], f.Name));
			}			
			
			// procs
			foreach (StoredProcedure p in db.StoredProcedures) {
				if (p.IsSystemObject) continue;
				urns.Clear();
				urns.Add(p.Urn);
				scr.Options.DriAll = true;
				ScriptToFile(scr, urns.ToArray(), String.Format("{0}/procs/{1}.sql", args[2], p.Name));
			}

			// TODO data

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
}
