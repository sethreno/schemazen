using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SchemaZen.model
{
	public class DatabaseDiff {
		public List<SqlAssembly> AssembliesAdded = new List<SqlAssembly>();
		public List<SqlAssembly> AssembliesDeleted = new List<SqlAssembly>();
		public List<SqlAssembly> AssembliesDiff = new List<SqlAssembly>();
		public List<ForeignKey> ForeignKeysAdded = new List<ForeignKey>();
		public List<ForeignKey> ForeignKeysDeleted = new List<ForeignKey>();
		public List<ForeignKey> ForeignKeysDiff = new List<ForeignKey>();
		public List<DbProp> PropsChanged = new List<DbProp>();

		public List<Routine> RoutinesAdded = new List<Routine>();
		public List<Routine> RoutinesDeleted = new List<Routine>();
		public List<Routine> RoutinesDiff = new List<Routine>();
		public List<Synonym> SynonymsAdded = new List<Synonym>();
		public List<Synonym> SynonymsDeleted = new List<Synonym>();
		public List<Synonym> SynonymsDiff = new List<Synonym>();
		public List<Table> TablesAdded = new List<Table>();
		public List<Table> TablesDeleted = new List<Table>();
		public List<TableDiff> TablesDiff = new List<TableDiff>();
		public List<Table> TableTypesDiff = new List<Table>();
		public List<SqlUser> UsersAdded = new List<SqlUser>();
		public List<SqlUser> UsersDeleted = new List<SqlUser>();
		public List<SqlUser> UsersDiff = new List<SqlUser>();
		public List<Constraint> ViewIndexesAdded = new List<Constraint>();
		public List<Constraint> ViewIndexesDeleted = new List<Constraint>();
		public List<Constraint> ViewIndexesDiff = new List<Constraint>();

		public DatabaseDiff (Database db1, Database db2) {
			//compare database properties           
			foreach (var p in from p in db1.Props
							  let p2 = db2.FindProp(p.Name)
							  where p.Script() != p2.Script()
							  select p) {
				PropsChanged.Add(p);
			}

			//get tables added and changed
			foreach (var tables in new[] {
											 db1.Tables,
											 db1.TableTypes
										 }) {
				foreach (var t in tables) {
					var t2 = db2.FindTable(t.Name, t.Owner, t.IsType);
					if (t2 == null) {
						TablesAdded.Add(t);
					} else {
						//compare mutual tables
						var tDiff = t.Compare(t2);
						if (tDiff.IsDiff) {
							if (t.IsType) {
								// types cannot be altered...
								TableTypesDiff.Add(t);
							} else {
								TablesDiff.Add(tDiff);
							}
						}
					}
				}
			}
			//get deleted tables
			foreach (var t in db2.Tables.Concat(db2.TableTypes).Where(t => db1.FindTable(t.Name, t.Owner, t.IsType) == null)) {
				TablesDeleted.Add(t);
			}

			//get procs added and changed
			foreach (var r in db1.Routines) {
				var r2 = db2.FindRoutine(r.Name, r.Owner);
				if (r2 == null) {
					RoutinesAdded.Add(r);
				} else {
					//compare mutual procs
					if (r.Text.Trim() != r2.Text.Trim()) {
						RoutinesDiff.Add(r);
					}
				}
			}
			//get procs deleted
			foreach (var r in db2.Routines.Where(r => db1.FindRoutine(r.Name, r.Owner) == null)) {
				RoutinesDeleted.Add(r);
			}

			//get added and compare mutual foreign keys
			foreach (var fk in db1.ForeignKeys) {
				var fk2 = db2.FindForeignKey(fk.Name);
				if (fk2 == null) {
					ForeignKeysAdded.Add(fk);
				} else {
					if (fk.ScriptCreate() != fk2.ScriptCreate()) {
						ForeignKeysDiff.Add(fk);
					}
				}
			}
			//get deleted foreign keys
			foreach (var fk in db2.ForeignKeys.Where(fk => db1.FindForeignKey(fk.Name) == null)) {
				ForeignKeysDeleted.Add(fk);
			}


			//get added and compare mutual assemblies
			foreach (var a in db1.Assemblies) {
				var a2 = db2.FindAssembly(a.Name);
				if (a2 == null) {
					AssembliesAdded.Add(a);
				} else {
					if (a.ScriptCreate() != a2.ScriptCreate()) {
						AssembliesDiff.Add(a);
					}
				}
			}
			//get deleted assemblies
			foreach (var a in db2.Assemblies.Where(a => db1.FindAssembly(a.Name) == null)) {
				AssembliesDeleted.Add(a);
			}


			//get added and compare mutual users
			foreach (var u in db1.Users) {
				var u2 = db2.FindUser(u.Name);
				if (u2 == null) {
					UsersAdded.Add(u);
				} else {
					if (u.ScriptCreate() != u2.ScriptCreate()) {
						UsersDiff.Add(u);
					}
				}
			}
			//get deleted users
			foreach (var u in db2.Users.Where(u => db1.FindUser(u.Name) == null)) {
				UsersDeleted.Add(u);
			}

			//get added and compare view indexes
			foreach (var c in db1.ViewIndexes) {
				var c2 = db2.FindViewIndex(c.Name);
				if (c2 == null) {
					ViewIndexesAdded.Add(c);
				} else {
					if (c.ScriptCreate() != c2.ScriptCreate()) {
						ViewIndexesDiff.Add(c);
					}
				}
			}
			//get deleted view indexes
			foreach (var c in db2.ViewIndexes.Where(c => db1.FindViewIndex(c.Name) == null)) {
				ViewIndexesDeleted.Add(c);
			}

			//get added and compare synonyms
			foreach (var s in db1.Synonyms) {
				var s2 = db2.FindSynonym(s.Name, s.Owner);
				if (s2 == null) {
					SynonymsAdded.Add(s);
				} else {
					if (s.BaseObjectName != s2.BaseObjectName) {
						SynonymsDiff.Add(s);
					}
				}
			}
			//get deleted synonyms
			foreach (var s in db2.Synonyms.Where(s => db1.FindSynonym(s.Name, s.Owner) == null)) {
				SynonymsDeleted.Add(s);
			}
		}

		public bool IsDiff {
			get {
				return PropsChanged.Count > 0 || TablesAdded.Count > 0 || TablesDiff.Count > 0 || TableTypesDiff.Count > 0 || TablesDeleted.Count > 0 || RoutinesAdded.Count > 0 || RoutinesDiff.Count > 0 || RoutinesDeleted.Count > 0 || ForeignKeysAdded.Count > 0 || ForeignKeysDiff.Count > 0 || ForeignKeysDeleted.Count > 0 || AssembliesAdded.Count > 0 || AssembliesDiff.Count > 0 || AssembliesDeleted.Count > 0 || UsersAdded.Count > 0 || UsersDiff.Count > 0 || UsersDeleted.Count > 0 || ViewIndexesAdded.Count > 0 || ViewIndexesDiff.Count > 0 || ViewIndexesDeleted.Count > 0 || SynonymsAdded.Count > 0 || SynonymsDiff.Count > 0 || SynonymsDeleted.Count > 0;
			}
		}

		private static string Summarize (bool includeNames, List<string> changes, string caption) {
			if (changes.Count == 0)
				return string.Empty;
			return changes.Count + "x " + caption + (includeNames ? ("\r\n\t" + string.Join("\r\n\t", changes.ToArray())) : string.Empty) + "\r\n";
		}

		public string SummarizeChanges (bool includeNames) {
			var sb = new StringBuilder();
			sb.Append(Summarize(includeNames, AssembliesAdded.Select(o => o.Name).ToList(), "assemblies in source but not in target"));
			sb.Append(Summarize(includeNames, AssembliesDeleted.Select(o => o.Name).ToList(), "assemblies not in source but in target"));
			sb.Append(Summarize(includeNames, AssembliesDiff.Select(o => o.Name).ToList(), "assemblies altered"));
			sb.Append(Summarize(includeNames, ForeignKeysAdded.Select(o => o.Name).ToList(), "foreign keys in source but not in target"));
			sb.Append(Summarize(includeNames, ForeignKeysDeleted.Select(o => o.Name).ToList(), "foreign keys not in source but in target"));
			sb.Append(Summarize(includeNames, ForeignKeysDiff.Select(o => o.Name).ToList(), "foreign keys altered"));
			sb.Append(Summarize(includeNames, PropsChanged.Select(o => o.Name).ToList(), "properties changed"));
			sb.Append(Summarize(includeNames, RoutinesAdded.Select(o => string.Format("{0} {1}.{2}", o.RoutineType.ToString(), o.Owner, o.Name)).ToList(), "routines in source but not in target"));
			sb.Append(Summarize(includeNames, RoutinesDeleted.Select(o => string.Format("{0} {1}.{2}", o.RoutineType.ToString(), o.Owner, o.Name)).ToList(), "routines not in source but in target"));
			sb.Append(Summarize(includeNames, RoutinesDiff.Select(o => string.Format("{0} {1}.{2}", o.RoutineType.ToString(), o.Owner, o.Name)).ToList(), "routines altered"));
			sb.Append(Summarize(includeNames, TablesAdded.Where(o => !o.IsType).Select(o => string.Format("{0}.{1}", o.Owner, o.Name)).ToList(), "tables in source but not in target"));
			sb.Append(Summarize(includeNames, TablesDeleted.Where(o => !o.IsType).Select(o => string.Format("{0}.{1}", o.Owner, o.Name)).ToList(), "tables not in source but in target"));
			sb.Append(Summarize(includeNames, TablesDiff.Select(o => string.Format("{0}.{1}", o.Owner, o.Name)).ToList(), "tables altered"));
			sb.Append(Summarize(includeNames, TablesAdded.Where(o => o.IsType).Select(o => string.Format("{0}.{1}", o.Owner, o.Name)).ToList(), "table types in source but not in target"));
			sb.Append(Summarize(includeNames, TablesDeleted.Where(o => o.IsType).Select(o => string.Format("{0}.{1}", o.Owner, o.Name)).ToList(), "table types not in source but in target"));
			sb.Append(Summarize(includeNames, TableTypesDiff.Select(o => string.Format("{0}.{1}", o.Owner, o.Name)).ToList(), "table types altered"));
			sb.Append(Summarize(includeNames, UsersAdded.Select(o => o.Name).ToList(), "users in source but not in target"));
			sb.Append(Summarize(includeNames, UsersDeleted.Select(o => o.Name).ToList(), "users not in source but in target"));
			sb.Append(Summarize(includeNames, UsersDiff.Select(o => o.Name).ToList(), "users altered"));
			sb.Append(Summarize(includeNames, ViewIndexesAdded.Select(o => o.Name).ToList(), "view indexes in source but not in target"));
			sb.Append(Summarize(includeNames, ViewIndexesDeleted.Select(o => o.Name).ToList(), "view indexes not in source but in target"));
			sb.Append(Summarize(includeNames, ViewIndexesDiff.Select(o => o.Name).ToList(), "view indexes altered"));
			sb.Append(Summarize(includeNames, SynonymsAdded.Select(o => string.Format("{0}.{1}", o.Owner, o.Name)).ToList(), "synonyms in source but not in target"));
			sb.Append(Summarize(includeNames, SynonymsDeleted.Select(o => string.Format("{0}.{1}", o.Owner, o.Name)).ToList(), "synonyms not in source but in target"));
			sb.Append(Summarize(includeNames, SynonymsDiff.Select(o => string.Format("{0}.{1}", o.Owner, o.Name)).ToList(), "synonyms altered"));
			return sb.ToString();
		}

		public string Script () {
			var text = new StringBuilder();
			//alter database props
			//TODO need to check dependencies for collation change
			//TODO how can collation be set to null at the server level?
			if (PropsChanged.Count > 0) {
				text.Append(Database.ScriptPropList(PropsChanged));
				text.AppendLine("GO");
				text.AppendLine();
			}

			//delete foreign keys
			if (ForeignKeysDeleted.Count + ForeignKeysDiff.Count > 0) {
				foreach (var fk in ForeignKeysDeleted) {
					text.AppendLine(fk.ScriptDrop());
				}
				//delete modified foreign keys
				foreach (var fk in ForeignKeysDiff) {
					text.AppendLine(fk.ScriptDrop());
				}
				text.AppendLine("GO");
			}

			//delete tables
			if (TablesDeleted.Count + TableTypesDiff.Count > 0) {
				foreach (var t in TablesDeleted.Concat(TableTypesDiff)) {
					text.AppendLine(t.ScriptDrop());
				}
				text.AppendLine("GO");
			}
			// TODO: table types drop will fail if anything references them... try to find a workaround?


			//modify tables
			if (TablesDiff.Count > 0) {
				foreach (var t in TablesDiff) {
					text.Append(t.Script());
				}
				text.AppendLine("GO");
			}

			//add tables
			if (TablesAdded.Count + TableTypesDiff.Count > 0) {
				foreach (var t in TablesAdded.Concat(TableTypesDiff)) {
					text.Append(t.ScriptCreate());
				}
				text.AppendLine("GO");
			}

			//add foreign keys
			if (ForeignKeysAdded.Count + ForeignKeysDiff.Count > 0) {
				foreach (var fk in ForeignKeysAdded) {
					text.AppendLine(fk.ScriptCreate());
				}
				//add modified foreign keys
				foreach (var fk in ForeignKeysDiff) {
					text.AppendLine(fk.ScriptCreate());
				}
				text.AppendLine("GO");
			}

			//add & delete procs, functions, & triggers
			foreach (var r in RoutinesAdded) {
				text.AppendLine(r.ScriptCreate());
				text.AppendLine("GO");
			}
			foreach (var r in RoutinesDiff) {
				// script alter if possible, otherwise drop and (re)create
				try {
					text.AppendLine(r.ScriptAlter());
					text.AppendLine("GO");
				} catch {
					text.AppendLine(r.ScriptDrop());
					text.AppendLine("GO");
					text.AppendLine(r.ScriptCreate());
					text.AppendLine("GO");
				}
			}
			foreach (var r in RoutinesDeleted) {
				text.AppendLine(r.ScriptDrop());
				text.AppendLine("GO");
			}

			//add & delete synonyms
			foreach (var s in SynonymsAdded) {
				text.AppendLine(s.ScriptCreate());
				text.AppendLine("GO");
			}
			foreach (var s in SynonymsDiff) {
				text.AppendLine(s.ScriptDrop());
				text.AppendLine("GO");
				text.AppendLine(s.ScriptCreate());
				text.AppendLine("GO");
			}
			foreach (var s in SynonymsDeleted) {
				text.AppendLine(s.ScriptDrop());
				text.AppendLine("GO");
			}

			return text.ToString();
		}
	}
}
