using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace model {
	using System.Text.RegularExpressions;

	public class Database {

		#region " Constructors "

		public Database() {

		}

		public Database(string name) {
			this.Name = name;
		}

		#endregion

		#region " Properties "

		public string Name;
		public List<Table> Tables = new List<Table>();
		public List<Routine> Routines = new List<Routine>();
		public List<ForeignKey> ForeignKeys = new List<ForeignKey>();

		public Table FindTable(string name) {
			foreach (Table t in Tables) {
				if (t.Name == name) return t;
			}
			return null;
		}

		public Table FindTableByPk(string name) {
			foreach (Table t in Tables) {
				if (t.PrimaryKey != null && t.PrimaryKey.Name == name) {
					return t;
				}
			}
			return null;
		}

		public Constraint FindConstraint(string name) {
			foreach (Table t in Tables) {
				foreach (Constraint c in t.Constraints) {
					if (c.Name == name) return c;
				}
			}
			return null;
		}

		public ForeignKey FindForeignKey(string name) {
			foreach (ForeignKey fk in ForeignKeys) {
				if (fk.Name == name) return fk;
			}
			return null;
		}

		public Routine FindRoutine(string name) {
			foreach (Routine r in Routines) {
				if (r.Name == name) return r;
			}
			return null;
		}

		public List<Table> FindTablesRegEx(string pattern) {
			List<Table> matches = new List<Table>();
			foreach (Table t in Tables) {
				if (Regex.Match(t.Name, pattern).Success) {
					matches.Add(t);
				}
			}
			return matches;
		}

		#endregion

		public void Load(string connString) {
			using (SqlConnection cn = new SqlConnection(connString)) {
				cn.Open();
				using (SqlCommand cm = cn.CreateCommand()) {
					//get tables
					cm.CommandText = @"
					select 
						TABLE_SCHEMA, 
						TABLE_NAME 
					from INFORMATION_SCHEMA.TABLES";
					using (SqlDataReader dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							Tables.Add(new Table((string)dr["TABLE_SCHEMA"], (string)dr["TABLE_NAME"]));
						}
					}

					//get columns
					cm.CommandText = @"
					select 
						TABLE_NAME,
						COLUMN_NAME,
						DATA_TYPE,
						IS_NULLABLE,
						CHARACTER_MAXIMUM_LENGTH,
						NUMERIC_PRECISION,
						NUMERIC_SCALE 
					from INFORMATION_SCHEMA.COLUMNS";
					using (SqlDataReader dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							Column c = new Column();
							c.Name = (string)dr["COLUMN_NAME"];
							c.Type = (string)dr["DATA_TYPE"];
							c.IsNullable = (string)dr["IS_NULLABLE"] == "YES";

							switch (c.Type) {
								case "binary":
								case "char":
								case "nchar":
								case "nvarchar":
								case "varbinary":
								case "varchar":
									c.Length = (int)dr["CHARACTER_MAXIMUM_LENGTH"];
									break;
								case "decimal":
								case "numeric":
									c.Precision = (byte)dr["NUMERIC_PRECISION"];
									c.Scale =  (int)dr["NUMERIC_SCALE"];
									break;
							}

							FindTable((string)dr["TABLE_NAME"]).Columns.Add(c);
						}
					}

					//get column identities
					cm.CommandText = @"
					select 
						t.name as TABLE_NAME, 
						c.name AS COLUMN_NAME,
						i.SEED_VALUE, i.INCREMENT_VALUE
					from sys.tables t 
						inner join sys.columns c on c.object_id = t.object_id
						inner join sys.identity_columns i on i.object_id = c.object_id
							and i.column_id = c.column_id";
					using (SqlDataReader dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							Table t = FindTable((string)dr["TABLE_NAME"]);
							t.Columns.Find((string)dr["COLUMN_NAME"]).Identity = 
								new Identity((int)dr["SEED_VALUE"], (int)dr["INCREMENT_VALUE"]);
						}
					}

					//get column defaults
					cm.CommandText = @"
					select 
						t.name as TABLE_NAME, 
						c.name as COLUMN_NAME, 
						d.name as DEFAULT_NAME, 
						d.definition as DEFAULT_VALUE
					from sys.tables t 
						inner join sys.columns c on c.object_id = t.object_id
						inner join sys.default_constraints d on c.column_id = d.parent_column_id
							and d.parent_object_id = c.object_id";
					using (SqlDataReader dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							Table t = FindTable((string)dr["TABLE_NAME"]);
							t.Columns.Find((string)dr["COLUMN_NAME"]).Default = 
								new Default((string)dr["DEFAULT_NAME"], (string)dr["DEFAULT_VALUE"]);
						}
					}

					//get constraints & indexes
					cm.CommandText = @"
					select 
						t.name as tableName, 
						i.name as indexName, 
						c.name as columnName,
						i.is_primary_key, 
						i.is_unique_constraint, 
						i.type_desc
					from sys.tables t 
						inner join sys.indexes i on i.object_id = t.object_id
						inner join sys.index_columns ic on ic.object_id = t.object_id
							and ic.index_id = i.index_id
						inner join sys.columns c on c.object_id = t.object_id
							and c.column_id = ic.column_id
					order by t.name, i.name, ic.key_ordinal";
					using (SqlDataReader dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							Constraint c = FindConstraint((string)dr["indexName"]);
							if (c == null) {
								c = new Constraint((string)dr["indexName"], "", "");
								Table t = FindTable((string)dr["tableName"]);
								t.Constraints.Add(c);
								c.Table = t;
							}
							c.Clustered = (string)dr["type_desc"] == "CLUSTERED";
							c.Columns.Add((string)dr["columnName"]);
							c.Type = "INDEX";
							if ((bool)dr["is_primary_key"]) c.Type = "PRIMARY KEY";
							if ((bool)dr["is_unique_constraint"]) c.Type = "UNIQUE";
						}
					}

					//get foreign keys
					cm.CommandText = @"
					select 
						TABLE_NAME, 
						CONSTRAINT_NAME 
					from INFORMATION_SCHEMA.TABLE_CONSTRAINTS
					where CONSTRAINT_TYPE = 'FOREIGN KEY'";
					using (SqlDataReader dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							Table t = FindTable((string)dr["TABLE_NAME"]);
							ForeignKey fk = new ForeignKey((string)dr["CONSTRAINT_NAME"]);
							fk.Table = t;
							ForeignKeys.Add(fk);
						}
					}

					//get foreign key props
					cm.CommandText = @"
					select 
						CONSTRAINT_NAME, 
						UNIQUE_CONSTRAINT_NAME, 
						UPDATE_RULE, 
						DELETE_RULE
					from INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS";
					using (SqlDataReader dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							ForeignKey fk = FindForeignKey((string)dr["CONSTRAINT_NAME"]);
							fk.RefTable = FindTableByPk((string)dr["UNIQUE_CONSTRAINT_NAME"]);
							fk.RefColumns = fk.RefTable.PrimaryKey.Columns;
							fk.OnUpdate = (string)dr["UPDATE_RULE"];
							fk.OnDelete = (string)dr["DELETE_RULE"];
						}
					}

					//get foreign key columns
					cm.CommandText = "select CONSTRAINT_NAME, COLUMN_NAME from INFORMATION_SCHEMA.KEY_COLUMN_USAGE";
					using (SqlDataReader dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							ForeignKey fk = FindForeignKey((string)dr["CONSTRAINT_NAME"]);
							if (fk != null) fk.Columns.Add((string)dr["COLUMN_NAME"]);
						}
					}

					//get routines
					cm.CommandText = @"
					select
						s.name as schemaName,
						o.name as routineName,
						o.type_desc,
						m.definition,
						t.name as tableName
					from sys.sql_modules m
						inner join sys.objects o on m.object_id = o.object_id
						inner join sys.schemas s on s.schema_id = o.schema_id
						left join sys.triggers tr on m.object_id = tr.object_id
						left join sys.tables t on tr.parent_id = t.object_id";
					using (SqlDataReader dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							Routine r = new Routine((string)dr["schemaName"], (string)dr["routineName"]);
							r.Text = (string)dr["definition"];
							Routines.Add(r);

							switch ((string)dr["type_desc"]) {
								case "SQL_STORED_PROCEDURE":
									r.Type = "PROCEDURE";
									break;
								case "SQL_TRIGGER":
									r.Type = "TRIGGER";
									break;
								case "SQL_SCALAR_FUNCTION":
									r.Type = "FUNCTION";
									break;
							}
						}
					}
				}
			}
		}

		public DatabaseDiff Compare(Database db) {
			DatabaseDiff diff = new DatabaseDiff();

			//get tables added and changed
			foreach (Table t in Tables) {
				Table t2 = db.FindTable(t.Name);
				if (t2 == null) {
					diff.TablesAdded.Add(t);
				} else {
					//compare mutual tables
					TableDiff tDiff = t.Compare(t2);
					if (tDiff.IsDiff) {
						diff.TablesDiff.Add(tDiff);
					}
				}
			}
			//get deleted tables
			foreach (Table t in db.Tables) {
				if (FindTable(t.Name) == null) {
					diff.TablesDeleted.Add(t);
				}
			}

			//get procs added and changed
			foreach (Routine r in Routines) {
				Routine r2 = db.FindRoutine(r.Name);
				if (r2 == null) {
					diff.RoutinesAdded.Add(r);
				} else {
					//compare mutual procs
					if (r.Text != r2.Text) {
						diff.RoutinesDiff.Add(r);
					}
				}
			}
			//get procs deleted
			foreach (Routine r in db.Routines) {
				if (FindRoutine(r.Name) == null) {
					diff.RoutinesDeleted.Add(r);
				}
			}

			//get added and compare mutual foreign keys
			foreach (ForeignKey fk in ForeignKeys) {
				ForeignKey fk2 = db.FindForeignKey(fk.Name);
				if (fk2 == null) {
					diff.ForeignKeysAdded.Add(fk);
				} else {
					if (fk.ScriptCreate() != fk2.ScriptCreate()) {
						diff.ForeignKeysChanged.Add(fk);
					}
				}
			}
			//get deleted foreign keys
			foreach (ForeignKey fk in db.ForeignKeys) {
				if (FindForeignKey(fk.Name) == null) {
					diff.ForeignKeysDeleted.Add(fk);
				}
			}

			return diff;
		}

		public string ScriptCreate() {
			StringBuilder text = new StringBuilder();

			text.AppendFormat("CREATE DATABASE {0}", Name);
			text.AppendLine();
			text.AppendLine("GO");
			text.AppendFormat("USE {0}", Name);
			text.AppendLine();
			text.AppendLine("GO");
			text.AppendLine();

			foreach (Table t in Tables) {
				text.AppendLine(t.ScriptCreate());
			}
			text.AppendLine();
			text.AppendLine("GO");

			foreach (ForeignKey fk in ForeignKeys) {
				text.AppendLine(fk.ScriptCreate());
			}
			text.AppendLine();
			text.AppendLine("GO");

			foreach (Routine r in Routines) {
				text.AppendLine(r.ScriptCreate());
				text.AppendLine();
				text.AppendLine("GO");
			}

			return text.ToString();
		}
	}

	public class DatabaseDiff {
		public List<Table> TablesAdded = new List<Table>();
		public List<TableDiff> TablesDiff = new List<TableDiff>();
		public List<Table> TablesDeleted = new List<Table>();

		public List<Routine> RoutinesAdded = new List<Routine>();
		public List<Routine> RoutinesDiff = new List<Routine>();
		public List<Routine> RoutinesDeleted = new List<Routine>();

		public List<ForeignKey> ForeignKeysAdded = new List<ForeignKey>();
		public List<ForeignKey> ForeignKeysChanged = new List<ForeignKey>();
		public List<ForeignKey> ForeignKeysDeleted = new List<ForeignKey>();

		public string Script() {
			StringBuilder text = new StringBuilder();
			//delete foreign keys
			foreach (ForeignKey fk in ForeignKeysDeleted) {
				text.AppendLine(fk.ScriptDrop());
				text.AppendLine();
			}
			//delete modified foreign keys
			foreach (ForeignKey fk in ForeignKeysChanged) {
				text.AppendLine(fk.ScriptDrop());
				text.AppendLine();
			}
			text.AppendLine("GO");

			//add tables
			foreach (Table t in TablesAdded) {
				text.AppendLine(t.ScriptCreate());
				text.AppendLine();
			}
			text.AppendLine("GO");

			//modify tables
			foreach (TableDiff t in TablesDiff) {
				text.AppendLine(t.Script());
			}
			text.AppendLine("GO");

			//delete tables
			foreach (Table t in TablesDeleted) {
				text.AppendLine(t.ScriptDrop());
			}
			text.AppendLine("GO");

			//add foreign keys
			foreach (ForeignKey fk in ForeignKeysAdded) {
				text.AppendLine(fk.ScriptCreate());
			}
			//add modified foreign keys
			foreach (ForeignKey fk in ForeignKeysChanged) {
				text.AppendLine(fk.ScriptCreate());
			}
			text.AppendLine("GO");

			//add & delete procs, functions, & triggers
			foreach (Routine r in RoutinesAdded) {
				text.AppendLine(r.ScriptCreate());
				text.AppendLine("GO");
			}
			foreach (Routine r in RoutinesDiff) {
				text.AppendLine(r.ScriptDrop());
				text.AppendLine("GO");
				text.AppendLine(r.ScriptCreate());
				text.AppendLine("GO");
			}

			return text.ToString();
		}
	}

}
