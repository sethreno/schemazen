using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace model
{
	public class Database
	{
		#region " Constructors "

		public Database()
		{
			this.Props.Add(new DbProp("COMPATIBILITY_LEVEL", ""));
			this.Props.Add(new DbProp("COLLATE", ""));
			this.Props.Add(new DbProp("AUTO_CLOSE", ""));
			this.Props.Add(new DbProp("AUTO_SHRINK", ""));
			this.Props.Add(new DbProp("ALLOW_SNAPSHOT_ISOLATION", ""));
			this.Props.Add(new DbProp("READ_COMMITTED_SNAPSHOT", ""));
			this.Props.Add(new DbProp("RECOVERY", ""));
			this.Props.Add(new DbProp("PAGE_VERIFY", ""));
			this.Props.Add(new DbProp("AUTO_CREATE_STATISTICS", ""));
			this.Props.Add(new DbProp("AUTO_UPDATE_STATISTICS", ""));
			this.Props.Add(new DbProp("AUTO_UPDATE_STATISTICS_ASYNC", ""));
			this.Props.Add(new DbProp("ANSI_NULL_DEFAULT", ""));
			this.Props.Add(new DbProp("ANSI_NULLS", ""));
			this.Props.Add(new DbProp("ANSI_PADDING", ""));
			this.Props.Add(new DbProp("ANSI_WARNINGS", ""));
			this.Props.Add(new DbProp("ARITHABORT", ""));
			this.Props.Add(new DbProp("CONCAT_NULL_YIELDS_NULL", ""));
			this.Props.Add(new DbProp("NUMERIC_ROUNDABORT", ""));
			this.Props.Add(new DbProp("QUOTED_IDENTIFIER", ""));
			this.Props.Add(new DbProp("RECURSIVE_TRIGGERS", ""));
			this.Props.Add(new DbProp("CURSOR_CLOSE_ON_COMMIT", ""));
			this.Props.Add(new DbProp("CURSOR_DEFAULT", ""));
			this.Props.Add(new DbProp("TRUSTWORTHY", ""));
			this.Props.Add(new DbProp("DB_CHAINING", ""));
			this.Props.Add(new DbProp("PARAMETERIZATION", ""));
			this.Props.Add(new DbProp("DATE_CORRELATION_OPTIMIZATION", ""));
		}

		public Database(string name)
			: this()
		{
			this.Name = name;
		}

		#endregion

		#region " Properties "

		public string Connection = "";
		public List<Table> DataTables = new List<Table>();
		public string Dir = "";
		public List<ForeignKey> ForeignKeys = new List<ForeignKey>();
		public string Name;

		public List<DbProp> Props = new List<DbProp>();
		public List<Routine> Routines = new List<Routine>();
		public List<Table> Tables = new List<Table>();
		public List<Schema> Schemas = new List<Schema>();

		public DbProp FindProp(string name)
		{
			return this.Props.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.CurrentCultureIgnoreCase));
		}

		public Table FindTable(string name, string owner)
		{
			return this.Tables.FirstOrDefault(t => t.Name == name && t.Owner == owner);
		}

		public Constraint FindConstraint(string name)
		{
			return this.Tables.SelectMany(t => t.Constraints).FirstOrDefault(c => c.Name == name);
		}

		public ForeignKey FindForeignKey(string name)
		{
			return this.ForeignKeys.FirstOrDefault(fk => fk.Name == name);
		}

		public Routine FindRoutine(string name, string schema)
		{
			return this.Routines.FirstOrDefault(r => r.Name == name && r.Schema == schema);
		}

		public List<Table> FindTablesRegEx(string pattern)
		{
			return this.Tables.Where(t => Regex.Match(t.Name, pattern).Success).ToList();
		}

		#endregion

		private static readonly string[] dirs = { "tables", "foreign_keys", "functions", "procedures", "triggers", "views", "xmlschemacollections" };

		private void SetPropOnOff(string propName, object dbVal)
		{
			if (dbVal != DBNull.Value)
			{
				this.FindProp(propName).Value = (bool)dbVal ? "ON" : "OFF";
			}
		}

		private void SetPropString(string propName, object dbVal)
		{
			if (dbVal != DBNull.Value)
			{
				this.FindProp(propName).Value = dbVal.ToString();
			}
		}

		public void Load()
		{
			var cnStrBuilder = new SqlConnectionStringBuilder(this.Connection);

			this.Tables.Clear();
			this.Routines.Clear();
			this.ForeignKeys.Clear();
			this.DataTables.Clear();
			using (var cn = new SqlConnection(this.Connection))
			{
				cn.Open();
				using (var cm = cn.CreateCommand())
				{
					// query schema for database properties
					cm.CommandText = @"
select
	[compatibility_level],
	[collation_name],
	[is_auto_close_on],
	[is_auto_shrink_on],
	[snapshot_isolation_state],
	[is_read_committed_snapshot_on],
	[recovery_model_desc],
	[page_verify_option_desc],
	[is_auto_create_stats_on],
	[is_auto_update_stats_on],
	[is_auto_update_stats_async_on],
	[is_ansi_null_default_on],
	[is_ansi_nulls_on],
	[is_ansi_padding_on],
	[is_ansi_warnings_on],
	[is_arithabort_on],
	[is_concat_null_yields_null_on],
	[is_numeric_roundabort_on],
	[is_quoted_identifier_on],
	[is_recursive_triggers_on],
	[is_cursor_close_on_commit_on],
	[is_local_cursor_default],
	[is_trustworthy_on],
	[is_db_chaining_on],
	[is_parameterization_forced],
	[is_date_correlation_on]
from sys.databases
where name = @dbname
";
					cm.Parameters.AddWithValue("@dbname", cnStrBuilder.InitialCatalog);
					using (IDataReader dr = cm.ExecuteReader())
					{
						if (dr.Read())
						{
							this.SetPropString("COMPATIBILITY_LEVEL", dr["compatibility_level"]);
							this.SetPropString("COLLATE", dr["collation_name"]);
							this.SetPropOnOff("AUTO_CLOSE", dr["is_auto_close_on"]);
							this.SetPropOnOff("AUTO_SHRINK", dr["is_auto_shrink_on"]);
							if (dr["snapshot_isolation_state"] != DBNull.Value)
							{
								this.FindProp("ALLOW_SNAPSHOT_ISOLATION").Value = (byte)dr["snapshot_isolation_state"] == 0 || (byte)dr["snapshot_isolation_state"] == 2 ? "OFF" : "ON";
							}
							this.SetPropOnOff("READ_COMMITTED_SNAPSHOT", dr["is_read_committed_snapshot_on"]);
							this.SetPropString("RECOVERY", dr["recovery_model_desc"]);
							this.SetPropString("PAGE_VERIFY", dr["page_verify_option_desc"]);
							this.SetPropOnOff("AUTO_CREATE_STATISTICS", dr["is_auto_create_stats_on"]);
							this.SetPropOnOff("AUTO_UPDATE_STATISTICS", dr["is_auto_update_stats_on"]);
							this.SetPropOnOff("AUTO_UPDATE_STATISTICS_ASYNC", dr["is_auto_update_stats_async_on"]);
							this.SetPropOnOff("ANSI_NULL_DEFAULT", dr["is_ansi_null_default_on"]);
							this.SetPropOnOff("ANSI_NULLS", dr["is_ansi_nulls_on"]);
							this.SetPropOnOff("ANSI_PADDING", dr["is_ansi_padding_on"]);
							this.SetPropOnOff("ANSI_WARNINGS", dr["is_ansi_warnings_on"]);
							this.SetPropOnOff("ARITHABORT", dr["is_arithabort_on"]);
							this.SetPropOnOff("CONCAT_NULL_YIELDS_NULL", dr["is_concat_null_yields_null_on"]);
							this.SetPropOnOff("NUMERIC_ROUNDABORT", dr["is_numeric_roundabort_on"]);
							this.SetPropOnOff("QUOTED_IDENTIFIER", dr["is_quoted_identifier_on"]);
							this.SetPropOnOff("RECURSIVE_TRIGGERS", dr["is_recursive_triggers_on"]);
							this.SetPropOnOff("CURSOR_CLOSE_ON_COMMIT", dr["is_cursor_close_on_commit_on"]);
							if (dr["is_local_cursor_default"] != DBNull.Value)
							{
								this.FindProp("CURSOR_DEFAULT").Value = (bool)dr["is_local_cursor_default"] ? "LOCAL" : "GLOBAL";
							}
							this.SetPropOnOff("TRUSTWORTHY", dr["is_trustworthy_on"]);
							this.SetPropOnOff("DB_CHAINING", dr["is_db_chaining_on"]);
							if (dr["is_parameterization_forced"] != DBNull.Value)
							{
								this.FindProp("PARAMETERIZATION").Value = (bool)dr["is_parameterization_forced"] ? "FORCED" : "SIMPLE";
							}
							this.SetPropOnOff("DATE_CORRELATION_OPTIMIZATION", dr["is_date_correlation_on"]);
						}
					}

					//get schemas
					cm.CommandText = @"
select s.name as schemaName, p.name as principalName
	from sys.schemas s
	inner join sys.database_principals p on s.principal_id = p.principal_id
	where s.schema_id < 16384
	and s.name not in ('dbo','guest','sys','INFORMATION_SCHEMA')
	order by schema_id
";
					using (var dr = cm.ExecuteReader())
					{
						while (dr.Read())
						{
							this.Schemas.Add(new Schema((string)dr["schemaName"], (string)dr["principalName"]));
						}
					}


					//get tables
					cm.CommandText = @"
					select 
						TABLE_SCHEMA, 
						TABLE_NAME 
					from INFORMATION_SCHEMA.TABLES
					where TABLE_TYPE = 'BASE TABLE'";
					using (var dr = cm.ExecuteReader())
					{
						while (dr.Read())
						{
							this.Tables.Add(new Table((string)dr["TABLE_SCHEMA"], (string)dr["TABLE_NAME"]));
						}
					}

					//get columns
					cm.CommandText = @"
					select 
						t.TABLE_SCHEMA,
						c.TABLE_NAME,
						c.COLUMN_NAME,
						c.DATA_TYPE,
						c.IS_NULLABLE,
						c.CHARACTER_MAXIMUM_LENGTH,
						c.NUMERIC_PRECISION,
						c.NUMERIC_SCALE 
					from INFORMATION_SCHEMA.COLUMNS c
						inner join INFORMATION_SCHEMA.TABLES t
								on t.TABLE_NAME = c.TABLE_NAME
									and t.TABLE_SCHEMA = c.TABLE_SCHEMA
									and t.TABLE_CATALOG = c.TABLE_CATALOG
					where
						t.TABLE_TYPE = 'BASE TABLE'
";
					using (var dr = cm.ExecuteReader())
					{
						while (dr.Read())
						{
							var c = new Column();
							c.Name = (string)dr["COLUMN_NAME"];
							c.Type = (string)dr["DATA_TYPE"];
							c.IsNullable = (string)dr["IS_NULLABLE"] == "YES";

							switch (c.Type)
							{
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
									c.Scale = (int)dr["NUMERIC_SCALE"];
									break;
							}

							this.FindTable((string)dr["TABLE_NAME"], (string)dr["TABLE_SCHEMA"]).Columns.Add(c);
						}
					}

					//get column identities
					cm.CommandText = @"
					select 
						s.name as TABLE_SCHEMA,
						t.name as TABLE_NAME, 
						c.name AS COLUMN_NAME,
						i.SEED_VALUE, i.INCREMENT_VALUE
					from sys.tables t 
						inner join sys.columns c on c.object_id = t.object_id
						inner join sys.identity_columns i on i.object_id = c.object_id
							and i.column_id = c.column_id
						inner join sys.schemas s on s.schema_id = t.schema_id ";
					using (var dr = cm.ExecuteReader())
					{
						while (dr.Read())
						{
							try
							{
								var t = this.FindTable((string)dr["TABLE_NAME"], (string)dr["TABLE_SCHEMA"]);
								var c = t.Columns.Find((string)dr["COLUMN_NAME"]);
								var seed = dr["SEED_VALUE"].ToString();
								var increment = dr["INCREMENT_VALUE"].ToString();
								c.Identity = new Identity(seed, increment);
							}
							catch (Exception ex)
							{
								throw new ApplicationException(string.Format("{0}.{1} : {2}", dr["TABLE_SCHEMA"], dr["TABLE_NAME"], ex.Message), ex);
							}
						}
					}

					//get column defaults
					cm.CommandText = @"
					select 
						s.name as TABLE_SCHEMA,
						t.name as TABLE_NAME, 
						c.name as COLUMN_NAME, 
						d.name as DEFAULT_NAME, 
						d.definition as DEFAULT_VALUE
					from sys.tables t 
						inner join sys.columns c on c.object_id = t.object_id
						inner join sys.default_constraints d on c.column_id = d.parent_column_id
							and d.parent_object_id = c.object_id
						inner join sys.schemas s on s.schema_id = t.schema_id";
					using (var dr = cm.ExecuteReader())
					{
						while (dr.Read())
						{
							var t = this.FindTable((string)dr["TABLE_NAME"], (string)dr["TABLE_SCHEMA"]);
							t.Columns.Find((string)dr["COLUMN_NAME"]).Default = new Default((string)dr["DEFAULT_NAME"], (string)dr["DEFAULT_VALUE"]);
						}
					}

					//get constraints & indexes
					cm.CommandText = @"
					select 
						s.name as schemaName,
						t.name as tableName, 
						t.baseType,
						i.name as indexName, 
						c.name as columnName,
						i.is_primary_key, 
						i.is_unique_constraint,
						i.is_unique, 
						i.type_desc,
						isnull(ic.is_included_column, 0) as is_included_column
					from (
						select object_id, name, schema_id, 'T' as baseType
						from   sys.tables
						union
						select object_id, name, schema_id, 'V' as baseType
						from   sys.views
						) t
						inner join sys.indexes i on i.object_id = t.object_id
						inner join sys.index_columns ic on ic.object_id = t.object_id
							and ic.index_id = i.index_id
						inner join sys.columns c on c.object_id = t.object_id
							and c.column_id = ic.column_id
						inner join sys.schemas s on s.schema_id = t.schema_id
					order by s.name, t.name, i.name, ic.key_ordinal, ic.index_column_id";
					using (var dr = cm.ExecuteReader())
					{
						while (dr.Read())
						{
							if ((string)dr["baseType"] == "V")
							{
								Console.WriteLine("Index {0} on view {1} has not been scripted as it is currently unsupported by {2}", dr["indexName"], dr["tableName"], System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
								continue;
							}
							var t = this.FindTable((string)dr["tableName"], (string)dr["schemaName"]);
							var c = t.FindConstraint((string)dr["indexName"]);
							if (c == null)
							{
								c = new Constraint((string)dr["indexName"], "", "");
								t.Constraints.Add(c);
								c.Table = t;
							}
							c.Clustered = (string)dr["type_desc"] == "CLUSTERED";
							c.Unique = (bool)dr["is_unique"];
							if ((bool)dr["is_included_column"])
							{
								c.IncludedColumns.Add((string)dr["columnName"]);
							}
							else
							{
								c.Columns.Add((string)dr["columnName"]);
							}

							c.Type = "INDEX";
							if ((bool)dr["is_primary_key"])
								c.Type = "PRIMARY KEY";
							if ((bool)dr["is_unique_constraint"])
								c.Type = "UNIQUE";
						}
					}

					//get foreign keys
					cm.CommandText = @"
					select 
						TABLE_SCHEMA,
						TABLE_NAME, 
						CONSTRAINT_NAME
					from INFORMATION_SCHEMA.TABLE_CONSTRAINTS
					where CONSTRAINT_TYPE = 'FOREIGN KEY'";
					using (var dr = cm.ExecuteReader())
					{
						while (dr.Read())
						{
							var t = this.FindTable((string)dr["TABLE_NAME"], (string)dr["TABLE_SCHEMA"]);
							var fk = new ForeignKey((string)dr["CONSTRAINT_NAME"]);
							fk.Table = t;
							this.ForeignKeys.Add(fk);
						}
					}

					//get foreign key props
					cm.CommandText = @"
select 
	CONSTRAINT_NAME, 
	UPDATE_RULE, 
	DELETE_RULE,
	fk.is_disabled
from INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
	inner join sys.foreign_keys fk on rc.CONSTRAINT_NAME = fk.name";
					using (var dr = cm.ExecuteReader())
					{
						while (dr.Read())
						{
							var fk = this.FindForeignKey((string)dr["CONSTRAINT_NAME"]);
							fk.OnUpdate = (string)dr["UPDATE_RULE"];
							fk.OnDelete = (string)dr["DELETE_RULE"];
							fk.Check = !(bool)dr["is_disabled"];
						}
					}

					//get foreign key columns and ref table
					cm.CommandText = @"
select
	fk.name as CONSTRAINT_NAME,
	c1.name as COLUMN_NAME,
	OBJECT_SCHEMA_NAME(fk.referenced_object_id) as REF_TABLE_SCHEMA,
	OBJECT_NAME(fk.referenced_object_id) as REF_TABLE_NAME,
	c2.name as REF_COLUMN_NAME
from sys.foreign_keys fk
inner join sys.foreign_key_columns fkc
	on fkc.constraint_object_id = fk.object_id
inner join sys.columns c1
	on fkc.parent_column_id = c1.column_id
	and fkc.parent_object_id = c1.object_id
inner join sys.columns c2
	on fkc.referenced_column_id = c2.column_id
	and fkc.referenced_object_id = c2.object_id
order by fk.name
";
					using (var dr = cm.ExecuteReader())
					{
						while (dr.Read())
						{
							var fk = this.FindForeignKey((string)dr["CONSTRAINT_NAME"]);
							if (fk == null)
							{
								continue;
							}
							fk.Columns.Add((string)dr["COLUMN_NAME"]);
							fk.RefColumns.Add((string)dr["REF_COLUMN_NAME"]);
							if (fk.RefTable == null)
							{
								fk.RefTable = this.FindTable((string)dr["REF_TABLE_NAME"], (string)dr["REF_TABLE_SCHEMA"]);
							}
						}
					}

					//get routines
					cm.CommandText = @"
					select
						s.name as schemaName,
						o.name as routineName,
						o.type_desc,
						m.definition,
						m.uses_ansi_nulls,
						m.uses_quoted_identifier,
						t.name as tableName
					from sys.sql_modules m
						inner join sys.objects o on m.object_id = o.object_id
						inner join sys.schemas s on s.schema_id = o.schema_id
						left join sys.triggers tr on m.object_id = tr.object_id
						left join sys.tables t on tr.parent_id = t.object_id";
					using (var dr = cm.ExecuteReader())
					{
						while (dr.Read())
						{
							var r = new Routine((string)dr["schemaName"], (string)dr["routineName"]);
							r.Text = (string)dr["definition"];
							r.AnsiNull = (bool)dr["uses_ansi_nulls"];
							r.QuotedId = (bool)dr["uses_quoted_identifier"];
							this.Routines.Add(r);

							switch ((string)dr["type_desc"])
							{
								case "SQL_STORED_PROCEDURE":
									r.RoutineType = Routine.RoutineKind.Procedure;
									break;
								case "SQL_TRIGGER":
									r.RoutineType = Routine.RoutineKind.Trigger;
									break;
								case "SQL_SCALAR_FUNCTION":
								case "SQL_INLINE_TABLE_VALUED_FUNCTION":
									r.RoutineType = Routine.RoutineKind.Function;
									break;
								case "VIEW":
									r.RoutineType = Routine.RoutineKind.View;
									break;
							}
						}
					}

					// get xml schemas
					cm.CommandText = @"select s.name as DBSchemaName, x.name as XMLSchemaCollectionName, xml_schema_namespace(s.name, x.name) as definition
from sys.xml_schema_collections x
join sys.schemas s on s.schema_id = x.schema_id
where s.name != 'sys'";
					using (var dr = cm.ExecuteReader())
					{
						while (dr.Read())
						{
							var r = new Routine((string)dr["DBSchemaName"], (string)dr["XMLSchemaCollectionName"])
									{
										Text = string.Format("CREATE XML SCHEMA COLLECTION {0}.{1} AS N'{2}'", (string)dr["DBSchemaName"], (string)dr["XMLSchemaCollectionName"], (string)dr["definition"]),
										RoutineType = Routine.RoutineKind.XmlSchemaCollection
									};
							this.Routines.Add(r);
						}
					}
				}
			}
		}

		public DatabaseDiff Compare(Database db)
		{
			var diff = new DatabaseDiff();
			diff.Db = db;

			//compare database properties           
			foreach (var p in from p in this.Props
							  let p2 = db.FindProp(p.Name)
							  where p.Script() != p2.Script()
							  select p)
			{
				diff.PropsChanged.Add(p);
			}

			//get tables added and changed
			foreach (var t in this.Tables)
			{
				var t2 = db.FindTable(t.Name, t.Owner);
				if (t2 == null)
				{
					diff.TablesAdded.Add(t);
				}
				else
				{
					//compare mutual tables
					var tDiff = t.Compare(t2);
					if (tDiff.IsDiff)
					{
						diff.TablesDiff.Add(tDiff);
					}
				}
			}
			//get deleted tables
			foreach (var t in db.Tables.Where(t => this.FindTable(t.Name, t.Owner) == null))
			{
				diff.TablesDeleted.Add(t);
			}

			//get procs added and changed
			foreach (var r in this.Routines)
			{
				var r2 = db.FindRoutine(r.Name, r.Schema);
				if (r2 == null)
				{
					diff.RoutinesAdded.Add(r);
				}
				else
				{
					//compare mutual procs
					if (r.Text != r2.Text)
					{
						diff.RoutinesDiff.Add(r);
					}
				}
			}
			//get procs deleted
			foreach (var r in db.Routines.Where(r => this.FindRoutine(r.Name, r.Schema) == null))
			{
				diff.RoutinesDeleted.Add(r);
			}

			//get added and compare mutual foreign keys
			foreach (var fk in this.ForeignKeys)
			{
				var fk2 = db.FindForeignKey(fk.Name);
				if (fk2 == null)
				{
					diff.ForeignKeysAdded.Add(fk);
				}
				else
				{
					if (fk.ScriptCreate() != fk2.ScriptCreate())
					{
						diff.ForeignKeysDiff.Add(fk);
					}
				}
			}
			//get deleted foreign keys
			foreach (var fk in db.ForeignKeys.Where(fk => this.FindForeignKey(fk.Name) == null))
			{
				diff.ForeignKeysDeleted.Add(fk);
			}

			return diff;
		}

		public string ScriptCreate()
		{
			var text = new StringBuilder();

			text.AppendFormat("CREATE DATABASE {0}", this.Name);
			text.AppendLine();
			text.AppendLine("GO");
			text.AppendFormat("USE {0}", this.Name);
			text.AppendLine();
			text.AppendLine("GO");
			text.AppendLine();

			if (this.Props.Count > 0)
			{
				text.Append(ScriptPropList(this.Props));
				text.AppendLine("GO");
				text.AppendLine();
			}

			if (this.Schemas.Count > 0)
			{
				text.Append(ScriptSchemas(this.Schemas));
				text.AppendLine("GO");
				text.AppendLine();
			}

			foreach (var t in this.Tables)
			{
				text.AppendLine(t.ScriptCreate());
			}
			text.AppendLine();
			text.AppendLine("GO");

			foreach (var fk in this.ForeignKeys)
			{
				text.AppendLine(fk.ScriptCreate());
			}
			text.AppendLine();
			text.AppendLine("GO");

			foreach (var r in this.Routines)
			{
				text.AppendLine(r.ScriptCreate(this));
				text.AppendLine();
				text.AppendLine("GO");
			}

			return text.ToString();
		}

		public void ScriptToDir()
		{
			if (Directory.Exists(this.Dir))
			{
				// delete the existing script files
				foreach (var f in dirs.Where(dir => Directory.Exists(this.Dir + "/" + dir)).SelectMany(dir => Directory.GetFiles(this.Dir + "/" + dir)))
				{
					File.Delete(f);
				}
			}
			// create dir tree
			foreach (var dir in dirs.Where(dir => !Directory.Exists(this.Dir + "/" + dir)))
			{
				Directory.CreateDirectory(this.Dir + "/" + dir);
			}

			var text = new StringBuilder();
			text.Append(ScriptPropList(this.Props));
			text.AppendLine("GO");
			text.AppendLine();
			File.WriteAllText(string.Format("{0}/props.sql", this.Dir),
				text.ToString());

			if (this.Schemas.Count > 0)
			{
				text = new StringBuilder();
				text.Append(ScriptSchemas(this.Schemas));
				text.AppendLine("GO");
				text.AppendLine();
				File.WriteAllText(string.Format("{0}/schemas.sql", this.Dir),
					text.ToString());

			}

			foreach (var t in this.Tables)
			{
				File.WriteAllText(
					string.Format("{0}/tables/{1}.sql", this.Dir, MakeFileName(t)),
					t.ScriptCreate() + "\r\nGO\r\n"
					);
			}

			foreach (var fk in this.ForeignKeys)
			{
				File.AppendAllText(
					string.Format("{0}/foreign_keys/{1}.sql", this.Dir, MakeFileName(fk.Table)),
					fk.ScriptCreate() + "\r\nGO\r\n"
					);
			}

			foreach (var r in this.Routines)
			{
				File.WriteAllText(
					string.Format("{0}/{1}/{2}.sql", this.Dir, r.RoutineType.ToString().ToLower() + "s", MakeFileName(r)),
					r.ScriptCreate(this) + "\r\nGO\r\n"
					);
			}

			this.ExportData();
		}

		private static string MakeFileName(Routine r)
		{
			return MakeFileName(r.Schema, r.Name);
		}

		private static string MakeFileName(Table t)
		{
			return MakeFileName(t.Owner, t.Name);
		}

		private static string MakeFileName(string schema, string name)
		{
			// Dont' include schema name for objects in the dbo schema.
			// This maintains backward compatability for those who use
			// SchemaZen to keep their schemas under version control.
			return schema.ToLower() == "dbo" ? name : string.Format("{0}.{1}", schema, name);
		}

		public void ExportData()
		{
			var dataDir = this.Dir + "/data";
			if (!Directory.Exists(dataDir))
			{
				Directory.CreateDirectory(dataDir);
			}
			foreach (var t in this.DataTables)
			{
				File.WriteAllText(dataDir + "/" + MakeFileName(t) + ".tsv", t.ExportData(this.Connection));
			}
		}

		public void ImportData()
		{
			var dataDir = this.Dir + "\\data";
			var tables = new List<Table>();
			if (!Directory.Exists(dataDir))
			{
				return;
			}

			foreach (var f in Directory.GetFiles(dataDir))
			{
				var fi = new FileInfo(f);
				var schema = "dbo";
				var table = Path.GetFileNameWithoutExtension(fi.Name);
				if (table.Contains("."))
				{
					schema = fi.Name.Split('.')[0];
					table = fi.Name.Split('.')[1];
				}
				var t = this.FindTable(table, schema);
				if (t == null)
				{
					continue;
				}
				try
				{
					t.ImportData(this.Connection, File.ReadAllText(fi.FullName));
				}
				catch (DataException ex)
				{
					throw new DataFileException(ex.Message, fi.FullName, ex.LineNumber);
				}
				catch (SqlBatchException ex)
				{
					throw new DataFileException(ex.Message, fi.FullName, ex.LineNumber);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error importing data for table " + fi.FullName);
					throw;
				}
			}
		}

		public void CreateFromDir(bool overwrite)
		{
			var cnBuilder = new SqlConnectionStringBuilder(this.Connection);
			if (DBHelper.DbExists(this.Connection))
			{
				DBHelper.DropDb(this.Connection);
			}

			Console.WriteLine("Creating database...");
			//create database
			DBHelper.CreateDb(this.Connection);

			//run scripts
			if (File.Exists(this.Dir + "/props.sql"))
			{
				try
				{
					DBHelper.ExecBatchSql(this.Connection, File.ReadAllText(this.Dir + "/props.sql"));
				}
				catch (SqlBatchException ex)
				{
					throw new SqlFileException(this.Dir + "/props.sql", ex);
				}

				// COLLATE can cause connection to be reset
				// so clear the pool so we get a new connection
				DBHelper.ClearPool(this.Connection);
			}

			if (File.Exists(this.Dir + "/schemas.sql"))
			{
				try
				{
					DBHelper.ExecBatchSql(this.Connection, File.ReadAllText(this.Dir + "/schemas.sql"));
				}
				catch (SqlBatchException ex)
				{
					throw new SqlFileException(this.Dir + "/schemas.sql", ex);
				}
			}

			Console.WriteLine("Creating database objects...");
			// create db objects
			// resolve dependencies by trying over and over
			// if the number of failures stops decreasing then give up
			var scripts = this.GetScripts();
			var errors = new List<SqlFileException>();
			var prevCount = int.MaxValue;
			while (scripts.Count > 0 && errors.Count < prevCount)
			{
				if (errors.Count > 0)
				{
					prevCount = errors.Count;
					Console.WriteLine(
						"{0} errors occurred, retrying...", errors.Count);
				}
				errors.Clear();
				foreach (var f in scripts.ToArray())
				{
					try
					{
						DBHelper.ExecBatchSql(this.Connection, File.ReadAllText(f));
						scripts.Remove(f);
					}
					catch (SqlBatchException ex)
					{
						errors.Add(new SqlFileException(f, ex));
						//Console.WriteLine("Error occurred in {0}: {1}", f, ex);
					}
				}
			}
			if (!errors.Any())
				Console.WriteLine("All errors resolved, were probably dependency issues...");
			Console.WriteLine();

			this.Load(); // load the schema first so we can import data
			Console.WriteLine("Importing data...");
			this.ImportData(); // load data

			Console.WriteLine("Data imported successfully.");
			if (Directory.Exists(this.Dir + "/after_data"))
			{
				foreach (var f in Directory.GetFiles(this.Dir + "/after_data", "*.sql"))
				{
					try
					{
						DBHelper.ExecBatchSql(this.Connection, File.ReadAllText(f));
					}
					catch (SqlBatchException ex)
					{
						errors.Add(new SqlFileException(f, ex));
					}
				}
			}

			Console.WriteLine("Adding foreign key constraints...");
			// foreign keys
			if (Directory.Exists(this.Dir + "/foreign_keys"))
			{
				foreach (var f in Directory.GetFiles(this.Dir + "/foreign_keys", "*.sql"))
				{
					try
					{
						DBHelper.ExecBatchSql(this.Connection, File.ReadAllText(f));
					}
					catch (SqlBatchException ex)
					{
						//throw new SqlFileException(f, ex);
						errors.Add(new SqlFileException(f, ex));
					}
				}
			}
			if (errors.Count > 0)
			{
				var ex = new BatchSqlFileException();
				ex.Exceptions = errors;
				throw ex;
			}
		}

		private List<string> GetScripts()
		{
			var scripts = new List<string>();
			foreach (var dirPath in dirs.Where(dir => "foreign_keys" != dir).Select(dir => this.Dir + "/" + dir).Where(Directory.Exists))
			{
				scripts.AddRange(Directory.GetFiles(dirPath, "*.sql"));
			}
			return scripts;
		}

		public void ExecCreate(bool dropIfExists)
		{
			var conStr = new SqlConnectionStringBuilder(this.Connection);
			var dbName = conStr.InitialCatalog;
			conStr.InitialCatalog = "master";
			if (DBHelper.DbExists(this.Connection))
			{
				if (dropIfExists)
				{
					DBHelper.DropDb(this.Connection);
				}
				else
				{
					throw new ApplicationException(string.Format("Database {0} {1} already exists.",
						conStr.DataSource, dbName));
				}
			}
			DBHelper.ExecBatchSql(conStr.ToString(), this.ScriptCreate());
		}

		public static string ScriptPropList(IList<DbProp> props)
		{
			var text = new StringBuilder();

			text.AppendLine("DECLARE @DB VARCHAR(255)");
			text.AppendLine("SET @DB = DB_NAME()");
			foreach (var p in props.Select(p => p.Script()).Where(p => !string.IsNullOrEmpty(p)))
			{
				text.AppendLine(p);
			}
			return text.ToString();
		}

		public static string ScriptSchemas(IList<Schema> schemas)
		{
			var text = new StringBuilder();
			foreach (var s in schemas)
			{
				var schemaName = s.Name.Replace("'", "''");
				var owner = s.Owner.Replace("'", "''");
				text.AppendFormat(@"
if not exists(select s.schema_id from sys.schemas s where s.name = '{0}') 
	and exists(select p.principal_id from sys.database_principals p where p.name = '{1}') begin
	exec sp_executesql N'create schema [{0}] authorization [{1}]'
end
", schemaName, owner);
			}
			return text.ToString();
		}
	}

	public class DatabaseDiff
	{
		public Database Db;
		public List<ForeignKey> ForeignKeysAdded = new List<ForeignKey>();
		public List<ForeignKey> ForeignKeysDeleted = new List<ForeignKey>();
		public List<ForeignKey> ForeignKeysDiff = new List<ForeignKey>();
		public List<DbProp> PropsChanged = new List<DbProp>();

		public List<Routine> RoutinesAdded = new List<Routine>();
		public List<Routine> RoutinesDeleted = new List<Routine>();
		public List<Routine> RoutinesDiff = new List<Routine>();
		public List<Table> TablesAdded = new List<Table>();
		public List<Table> TablesDeleted = new List<Table>();
		public List<TableDiff> TablesDiff = new List<TableDiff>();

		public bool IsDiff
		{
			get
			{
				return this.PropsChanged.Count > 0
					   || this.TablesAdded.Count > 0
					   || this.TablesDiff.Count > 0
					   || this.TablesDeleted.Count > 0
					   || this.RoutinesAdded.Count > 0
					   || this.RoutinesDiff.Count > 0
					   || this.RoutinesDeleted.Count > 0
					   || this.ForeignKeysAdded.Count > 0
					   || this.ForeignKeysDiff.Count > 0
					   || this.ForeignKeysDeleted.Count > 0;
			}
		}

		public string Script()
		{
			var text = new StringBuilder();
			//alter database props
			//TODO need to check dependencies for collation change
			//TODO how can collation be set to null at the server level?
			if (this.PropsChanged.Count > 0)
			{
				text.Append(Database.ScriptPropList(this.PropsChanged));
				text.AppendLine("GO");
				text.AppendLine();
			}

			//delete foreign keys
			if (this.ForeignKeysDeleted.Count + this.ForeignKeysDiff.Count > 0)
			{
				foreach (var fk in this.ForeignKeysDeleted)
				{
					text.AppendLine(fk.ScriptDrop());
				}
				//delete modified foreign keys
				foreach (var fk in this.ForeignKeysDiff)
				{
					text.AppendLine(fk.ScriptDrop());
				}
				text.AppendLine("GO");
			}

			//add tables
			if (this.TablesAdded.Count > 0)
			{
				foreach (var t in this.TablesAdded)
				{
					text.Append(t.ScriptCreate());
				}
				text.AppendLine("GO");
			}

			//modify tables
			if (this.TablesDiff.Count > 0)
			{
				foreach (var t in this.TablesDiff)
				{
					text.Append(t.Script());
				}
				text.AppendLine("GO");
			}

			//delete tables
			if (this.TablesDeleted.Count > 0)
			{
				foreach (var t in this.TablesDeleted)
				{
					text.AppendLine(t.ScriptDrop());
				}
				text.AppendLine("GO");
			}

			//add foreign keys
			if (this.ForeignKeysAdded.Count + this.ForeignKeysDiff.Count > 0)
			{
				foreach (var fk in this.ForeignKeysAdded)
				{
					text.AppendLine(fk.ScriptCreate());
				}
				//add modified foreign keys
				foreach (var fk in this.ForeignKeysDiff)
				{
					text.AppendLine(fk.ScriptCreate());
				}
				text.AppendLine("GO");
			}

			//add & delete procs, functions, & triggers
			foreach (var r in this.RoutinesAdded)
			{
				text.AppendLine(r.ScriptCreate(this.Db));
				text.AppendLine("GO");
			}
			foreach (var r in this.RoutinesDiff)
			{
				text.AppendLine(r.ScriptDrop());
				text.AppendLine("GO");
				text.AppendLine(r.ScriptCreate(this.Db));
				text.AppendLine("GO");
			}
			foreach (var r in this.RoutinesDeleted)
			{
				text.AppendLine(r.ScriptDrop());
				text.AppendLine("GO");
			}

			return text.ToString();
		}
	}
}