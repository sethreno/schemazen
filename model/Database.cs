using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SchemaZen.model {
	public class Database {
		#region " Constructors "

		public Database() {
			Props.Add(new DbProp("COMPATIBILITY_LEVEL", ""));
			Props.Add(new DbProp("COLLATE", ""));
			Props.Add(new DbProp("AUTO_CLOSE", ""));
			Props.Add(new DbProp("AUTO_SHRINK", ""));
			Props.Add(new DbProp("ALLOW_SNAPSHOT_ISOLATION", ""));
			Props.Add(new DbProp("READ_COMMITTED_SNAPSHOT", ""));
			Props.Add(new DbProp("RECOVERY", ""));
			Props.Add(new DbProp("PAGE_VERIFY", ""));
			Props.Add(new DbProp("AUTO_CREATE_STATISTICS", ""));
			Props.Add(new DbProp("AUTO_UPDATE_STATISTICS", ""));
			Props.Add(new DbProp("AUTO_UPDATE_STATISTICS_ASYNC", ""));
			Props.Add(new DbProp("ANSI_NULL_DEFAULT", ""));
			Props.Add(new DbProp("ANSI_NULLS", ""));
			Props.Add(new DbProp("ANSI_PADDING", ""));
			Props.Add(new DbProp("ANSI_WARNINGS", ""));
			Props.Add(new DbProp("ARITHABORT", ""));
			Props.Add(new DbProp("CONCAT_NULL_YIELDS_NULL", ""));
			Props.Add(new DbProp("NUMERIC_ROUNDABORT", ""));
			Props.Add(new DbProp("QUOTED_IDENTIFIER", ""));
			Props.Add(new DbProp("RECURSIVE_TRIGGERS", ""));
			Props.Add(new DbProp("CURSOR_CLOSE_ON_COMMIT", ""));
			Props.Add(new DbProp("CURSOR_DEFAULT", ""));
			Props.Add(new DbProp("TRUSTWORTHY", ""));
			Props.Add(new DbProp("DB_CHAINING", ""));
			Props.Add(new DbProp("PARAMETERIZATION", ""));
			Props.Add(new DbProp("DATE_CORRELATION_OPTIMIZATION", ""));
		}

		public Database(string name)
			: this() {
			Name = name;
		}

		#endregion

		public const string SqlWhitespaceOrCommentRegex = @"(?:--.*?(?:\r|\n)|/\*.*?\*/|\s+?)";
		public const string SqlEnclosedIdentifierRegex = @"\[.+?\]";
		public const string SqlQuotedIdentifierRegex = "\".+?\"";

		#region " Properties "

		public List<SqlAssembly> Assemblies = new List<SqlAssembly>();
		public string Connection = "";
		public List<Table> DataTables = new List<Table>();
		public string Dir = "";
		public List<ForeignKey> ForeignKeys = new List<ForeignKey>();
		public string Name;

		public List<DbProp> Props = new List<DbProp>();
		public List<Routine> Routines = new List<Routine>();
		public List<Schema> Schemas = new List<Schema>();
		public List<Synonym> Synonyms = new List<Synonym>();
		public List<Table> TableTypes = new List<Table>();
		public List<Table> Tables = new List<Table>();
		public List<SqlUser> Users = new List<SqlUser>();
		public List<Constraint> ViewIndexes = new List<Constraint>();

		public DbProp FindProp(string name) {
			return Props.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.CurrentCultureIgnoreCase));
		}

		public Table FindTable(string name, string owner, bool isTableType = false)
		{
			return FindTableBase(isTableType ? TableTypes : Tables, name, owner);
		}

		private static Table FindTableBase(IEnumerable<Table> tables, string name, string owner)
		{
			return tables.FirstOrDefault(t => t.Name == name && t.Owner == owner);
		}

		public Constraint FindConstraint(string name) {
			return Tables.SelectMany(t => t.Constraints).FirstOrDefault(c => c.Name == name);
		}

		public ForeignKey FindForeignKey(string name) {
			return ForeignKeys.FirstOrDefault(fk => fk.Name == name);
		}

		public Routine FindRoutine(string name, string schema) {
			return Routines.FirstOrDefault(r => r.Name == name && r.Schema == schema);
		}

		public SqlAssembly FindAssembly(string name) {
			return Assemblies.FirstOrDefault(a => a.Name == name);
		}

		public SqlUser FindUser(string name) {
			return Users.FirstOrDefault(u => u.Name == name);
		}

		public Constraint FindViewIndex(string name) {
			return ViewIndexes.FirstOrDefault(c => c.Name == name);
		}

		public Synonym FindSynonym(string name, string schema)
		{
			return Synonyms.FirstOrDefault(s => s.Name == name && s.Schema == schema);
		}

		public List<Table> FindTablesRegEx(string pattern) {
			return Tables.Where(t => Regex.Match(t.Name, pattern).Success).ToList();
		}

		#endregion

		private static readonly string[] dirs = {
			"tables", "foreign_keys", "assemblies", "functions", "procedures", "triggers",
			"views", "xmlschemacollections", "data", "users", "synonyms"
		};

		private void SetPropOnOff(string propName, object dbVal) {
			if (dbVal != DBNull.Value) {
				FindProp(propName).Value = (bool) dbVal ? "ON" : "OFF";
			}
		}

		private void SetPropString(string propName, object dbVal) {
			if (dbVal != DBNull.Value) {
				FindProp(propName).Value = dbVal.ToString();
			}
		}

		#region Load
		public void Load() {
			Tables.Clear();
			Routines.Clear();
			ForeignKeys.Clear();
			DataTables.Clear();
			ViewIndexes.Clear();
			Assemblies.Clear();
			Users.Clear();
			Synonyms.Clear();

			using (var cn = new SqlConnection(Connection)) {
				cn.Open();
				using (SqlCommand cm = cn.CreateCommand()) {
					LoadProps(cm);
					LoadSchemas(cm);
					LoadTables(cm);
					LoadColumns(cm);
					LoadColumnIdentities(cm);
					LoadColumnDefaults(cm);
					LoadColumnComputes(cm);
					LoadConstraintsAndIndexes(cm);
					LoadForeignKeys(cm);
					LoadRoutines(cm);
					LoadXmlSchemas(cm);
					LoadCLRAssemblies(cm);
					LoadUsersAndLogins(cm);
					LoadSynonyms(cm);
				}
			}
		}

		private void LoadSynonyms(SqlCommand cm)
		{
			try
			{
				// get synonyms
				cm.CommandText = @"
						select object_schema_name(object_id) as schema_name, name as synonym_name, base_object_name
						from sys.synonyms";
				using (SqlDataReader dr = cm.ExecuteReader())
				{
					while (dr.Read())
					{
						Synonym synonym = new Synonym((string)dr["synonym_name"], (string)dr["schema_name"]);
						synonym.BaseObjectName = (string)dr["base_object_name"];
						Synonyms.Add(synonym);
					}
				}
			}
			catch (SqlException)
			{
				// SQL server version doesn't support synonyms, nothing to do here
			}
		}

		private void LoadUsersAndLogins(SqlCommand cm)
		{
			// get users that have access to the database
			cm.CommandText = @"
					select dp.name as UserName, USER_NAME(drm.role_principal_id) as AssociatedDBRole, default_schema_name
					from sys.database_principals dp
					left outer join sys.database_role_members drm on dp.principal_id = drm.member_principal_id
					where dp.type_desc = 'SQL_USER'
					and dp.sid not in (0x00, 0x01) --ignore guest and dbo
					and dp.is_fixed_role = 0
					order by dp.name";
			SqlUser u = null;
			using (SqlDataReader dr = cm.ExecuteReader())
			{
				while (dr.Read())
				{
					if (u == null || u.Name != (string)dr["UserName"])
						u = new SqlUser((string)dr["UserName"], (string)dr["default_schema_name"]);
					if (!(dr["AssociatedDBRole"] is DBNull))
						u.DatabaseRoles.Add((string)dr["AssociatedDBRole"]);
					if (!Users.Contains(u))
						Users.Add(u);
				}
			}

			// get sql logins
			cm.CommandText = @"
					select sp.name,  sl.password_hash
					from sys.server_principals sp
					inner join sys.sql_logins sl on sp.principal_id = sl.principal_id and sp.type_desc = 'SQL_LOGIN'
					where sp.name not like '##%##'
					and sp.name != 'SA'
					order by sp.name";
			using (SqlDataReader dr = cm.ExecuteReader())
			{
				while (dr.Read())
				{
					u = FindUser((string)dr["name"]);
					if (u != null && !(dr["password_hash"] is DBNull))
						u.PasswordHash = (byte[])dr["password_hash"];
				}
			}
		}

		private void LoadCLRAssemblies(SqlCommand cm)
		{
			try
			{
				// get CLR assemblies
				cm.CommandText = @"select a.name as AssemblyName, a.permission_set_desc, af.name as FileName, af.content
						from sys.assemblies a
						inner join sys.assembly_files af on a.assembly_id = af.assembly_id 
						where a.is_user_defined = 1
						order by a.name, af.file_id";
				SqlAssembly a = null;
				using (SqlDataReader dr = cm.ExecuteReader())
				{
					while (dr.Read())
					{
						if (a == null || a.Name != (string)dr["AssemblyName"])
							a = new SqlAssembly((string)dr["permission_set_desc"], (string)dr["AssemblyName"]);
						a.Files.Add(new KeyValuePair<string, byte[]>((string)dr["FileName"], (byte[])dr["content"]));
						if (!Assemblies.Contains(a))
							Assemblies.Add(a);
					}
				}
			}
			catch (SqlException)
			{
				// SQL server version doesn't support CLR assemblies, nothing to do here
			}
		}

		private void LoadXmlSchemas(SqlCommand cm)
		{
			try
			{
				// get xml schemas
				cm.CommandText = @"
						select s.name as DBSchemaName, x.name as XMLSchemaCollectionName, xml_schema_namespace(s.name, x.name) as definition
						from sys.xml_schema_collections x
						inner join sys.schemas s on s.schema_id = x.schema_id
						where s.name != 'sys'";
				using (SqlDataReader dr = cm.ExecuteReader())
				{
					while (dr.Read())
					{
						var r = new Routine((string)dr["DBSchemaName"], (string)dr["XMLSchemaCollectionName"])
						{
							Text = string.Format("CREATE XML SCHEMA COLLECTION {0}.{1} AS N'{2}'", dr["DBSchemaName"], dr["XMLSchemaCollectionName"], dr["definition"]),
							RoutineType = Routine.RoutineKind.XmlSchemaCollection
						};
						Routines.Add(r);
					}
				}
			}
			catch (SqlException)
			{
				// SQL server version doesn't support XML schemas, nothing to do here
			}
		}

		private void LoadRoutines(SqlCommand cm)
		{
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
						left join sys.tables t on tr.parent_id = t.object_id
					where objectproperty(o.object_id, 'IsMSShipped') = 0
					";
			using (SqlDataReader dr = cm.ExecuteReader())
			{
				while (dr.Read())
				{
					// TODO: consider refactoring - it is bad practice to write to the console from a class library...
					if (dr["definition"] is DBNull)
					{
						Console.ForegroundColor = ConsoleColor.Magenta;
						Console.WriteLine("Warning: Unable to get definition for {0} {1}.{2}", (string)dr["type_desc"], (string)dr["schemaName"], (string)dr["routineName"]);
						Console.ForegroundColor = ConsoleColor.White;
					}
					else
					{
						var r = new Routine((string)dr["schemaName"], (string)dr["routineName"]);
						r.Text = (string)dr["definition"];
						r.AnsiNull = (bool)dr["uses_ansi_nulls"];
						r.QuotedId = (bool)dr["uses_quoted_identifier"];
						Routines.Add(r);

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
			}
		}

		private void LoadForeignKeys(SqlCommand cm)
		{
			//get foreign keys
			cm.CommandText = @"
					select 
						TABLE_SCHEMA,
						TABLE_NAME, 
						CONSTRAINT_NAME
					from INFORMATION_SCHEMA.TABLE_CONSTRAINTS
					where CONSTRAINT_TYPE = 'FOREIGN KEY'";
			using (SqlDataReader dr = cm.ExecuteReader())
			{
				while (dr.Read())
				{
					Table t = FindTable((string)dr["TABLE_NAME"], (string)dr["TABLE_SCHEMA"]);
					var fk = new ForeignKey((string)dr["CONSTRAINT_NAME"]);
					fk.Table = t;
					ForeignKeys.Add(fk);
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
			using (SqlDataReader dr = cm.ExecuteReader())
			{
				while (dr.Read())
				{
					ForeignKey fk = FindForeignKey((string)dr["CONSTRAINT_NAME"]);
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
order by fk.name, fkc.constraint_column_id
";
			using (SqlDataReader dr = cm.ExecuteReader())
			{
				while (dr.Read())
				{
					ForeignKey fk = FindForeignKey((string)dr["CONSTRAINT_NAME"]);
					if (fk == null)
					{
						continue;
					}
					fk.Columns.Add((string)dr["COLUMN_NAME"]);
					fk.RefColumns.Add((string)dr["REF_COLUMN_NAME"]);
					if (fk.RefTable == null)
					{
						fk.RefTable = FindTable((string)dr["REF_TABLE_NAME"], (string)dr["REF_TABLE_SCHEMA"]);
					}
				}
			}
		}

		private void LoadConstraintsAndIndexes(SqlCommand cm)
		{
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
			using (SqlDataReader dr = cm.ExecuteReader())
			{
				while (dr.Read())
				{
					Table t = (string)dr["baseType"] == "V"
						? new Table((string)dr["schemaName"], (string)dr["tableName"])
						: FindTable((string)dr["tableName"], (string)dr["schemaName"]);
					Constraint c = t.FindConstraint((string)dr["indexName"]);
					if (c == null)
					{
						c = new Constraint((string)dr["indexName"], "", "");
						t.Constraints.Add(c);
						c.Table = t;

						if ((string)dr["baseType"] == "V")
							ViewIndexes.Add(c);
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
		}

		private void LoadColumnComputes(SqlCommand cm)
		{
			//get computed column definitions
			cm.CommandText = @"
					select
						object_schema_name(object_id) as TABLE_SCHEMA,
						object_name(object_id) as TABLE_NAME,
						name as COLUMN_NAME,
						definition as DEFINITION
					from sys.computed_columns cc
					";
			using (SqlDataReader dr = cm.ExecuteReader())
			{
				while (dr.Read())
				{
					Table t = FindTable((string)dr["TABLE_NAME"], (string)dr["TABLE_SCHEMA"]);
					t.Columns.Find((string)dr["COLUMN_NAME"]).ComputedDefinition = (string)dr["DEFINITION"];
				}
			}
		}

		private void LoadColumnDefaults(SqlCommand cm) {
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
			using (SqlDataReader dr = cm.ExecuteReader())
			{
				while (dr.Read())
				{
					Table t = FindTable((string)dr["TABLE_NAME"], (string)dr["TABLE_SCHEMA"]);
					t.Columns.Find((string)dr["COLUMN_NAME"]).Default = new Default((string)dr["DEFAULT_NAME"],
						(string)dr["DEFAULT_VALUE"]);
				}
			}
		}

		private void LoadColumnIdentities(SqlCommand cm) {
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
			using (SqlDataReader dr = cm.ExecuteReader())
			{
				while (dr.Read())
				{
					try
					{
						Table t = FindTable((string)dr["TABLE_NAME"], (string)dr["TABLE_SCHEMA"]);
						Column c = t.Columns.Find((string)dr["COLUMN_NAME"]);
						string seed = dr["SEED_VALUE"].ToString();
						string increment = dr["INCREMENT_VALUE"].ToString();
						c.Identity = new Identity(seed, increment);
					}
					catch (Exception ex)
					{
						throw new ApplicationException(
							string.Format("{0}.{1} : {2}", dr["TABLE_SCHEMA"], dr["TABLE_NAME"], ex.Message), ex);
					}
				}
			}
		}

		private void LoadColumns(SqlCommand cm)
		{
			//get columns
			cm.CommandText = @"
				select 
					t.TABLE_SCHEMA,
					c.TABLE_NAME,
					c.COLUMN_NAME,
					c.DATA_TYPE,
					c.ORDINAL_POSITION,
					c.IS_NULLABLE,
					c.CHARACTER_MAXIMUM_LENGTH,
					c.NUMERIC_PRECISION,
					c.NUMERIC_SCALE,
					CASE WHEN COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsRowGuidCol') = 1 THEN 'YES' ELSE 'NO' END AS IS_ROW_GUID_COL
				from INFORMATION_SCHEMA.COLUMNS c
					inner join INFORMATION_SCHEMA.TABLES t
							on t.TABLE_NAME = c.TABLE_NAME
								and t.TABLE_SCHEMA = c.TABLE_SCHEMA
								and t.TABLE_CATALOG = c.TABLE_CATALOG
				where
					t.TABLE_TYPE = 'BASE TABLE'
				order by t.TABLE_SCHEMA, c.TABLE_NAME, c.ORDINAL_POSITION
";
			using (SqlDataReader dr = cm.ExecuteReader()) {
				LoadColumnsBase(dr, Tables);
			}

			try {
				cm.CommandText = @"
				select 
					s.name as TABLE_SCHEMA,
					tt.name as TABLE_NAME, 
					c.name as COLUMN_NAME,
					t.name as DATA_TYPE,
					c.column_id as ORDINAL_POSITION,
					CASE WHEN c.is_nullable = 1 THEN 'YES' ELSE 'NO' END as IS_NULLABLE,
					CAST(c.max_length as int) as CHARACTER_MAXIMUM_LENGTH,
					c.precision as NUMERIC_PRECISION,
					c.scale as NUMERIC_SCALE,
					CASE WHEN c.is_rowguidcol = 1 THEN 'YES' ELSE 'NO' END as IS_ROW_GUID_COL
				from sys.columns c
					inner join sys.table_types tt
						on tt.type_table_object_id = c.object_id
					inner join sys.schemas s
						on tt.schema_id = s.schema_id 
					inner join sys.types t
						on t.system_type_id = c.system_type_id
							and t.user_type_id = c.user_type_id
				where
					tt.is_user_defined = 1
				order by s.name, tt.name, c.column_id
";
				using (SqlDataReader dr = cm.ExecuteReader()) {
					LoadColumnsBase(dr, TableTypes);
				}
			} catch (SqlException) {
				// SQL server version doesn't support table types, nothing to do
			}

		}

		private static void LoadColumnsBase(IDataReader dr, List<Table> tables) {
			Table table = null;

			while (dr.Read()) {
				var c = new Column {
					Name = (string) dr["COLUMN_NAME"],
					Type = (string) dr["DATA_TYPE"],
					IsNullable = (string) dr["IS_NULLABLE"] == "YES",
					Position = (int) dr["ORDINAL_POSITION"],
					IsRowGuidCol = (string) dr["IS_ROW_GUID_COL"] == "YES"
				};

				switch (c.Type) {
					case "binary":
					case "char":
					case "nchar":
					case "nvarchar":
					case "varbinary":
					case "varchar":
						c.Length = (int) dr["CHARACTER_MAXIMUM_LENGTH"];
						break;
					case "decimal":
					case "numeric":
						c.Precision = (byte) dr["NUMERIC_PRECISION"];
						c.Scale = (int) dr["NUMERIC_SCALE"];
						break;
				}

				if (table == null || table.Name != (string) dr["TABLE_NAME"] || table.Owner != (string) dr["TABLE_SCHEMA"])
					// only do a lookup if the table we have isn't already the relevant one
					table = FindTableBase(tables, (string) dr["TABLE_NAME"], (string) dr["TABLE_SCHEMA"]);
				table.Columns.Add(c);
			}
		}

		private void LoadTables(SqlCommand cm)
		{
			//get tables
			cm.CommandText = @"
				select 
					TABLE_SCHEMA, 
					TABLE_NAME 
				from INFORMATION_SCHEMA.TABLES
				where TABLE_TYPE = 'BASE TABLE'";
			using (SqlDataReader dr = cm.ExecuteReader())
			{
				LoadTablesBase(dr, false, Tables);
			}

			//get table types
			try {
				cm.CommandText = @"
				select 
					s.name as TABLE_SCHEMA,
					tt.name as TABLE_NAME
				from sys.table_types tt
				inner join sys.schemas s on tt.schema_id = s.schema_id
				where tt.is_user_defined = 1
				order by s.name, tt.name";
				using (SqlDataReader dr = cm.ExecuteReader()) {
					LoadTablesBase(dr, true, TableTypes);
				}
			} catch (SqlException) {
				// SQL server version doesn't support table types, nothing to do here
			}
		}

		private static void LoadTablesBase(SqlDataReader dr, bool areTableTypes, List<Table> tables)
		{
			while (dr.Read()) {
				tables.Add(new Table((string) dr["TABLE_SCHEMA"], (string) dr["TABLE_NAME"]) {IsType = areTableTypes});
			}
		}

		private void LoadSchemas(SqlCommand cm)
		{
			//get schemas
			cm.CommandText = @"
					select s.name as schemaName, p.name as principalName
					from sys.schemas s
					inner join sys.database_principals p on s.principal_id = p.principal_id
					where s.schema_id < 16384
					and s.name not in ('dbo','guest','sys','INFORMATION_SCHEMA')
					order by schema_id
";
			using (SqlDataReader dr = cm.ExecuteReader())
			{
				while (dr.Read())
				{
					Schemas.Add(new Schema((string)dr["schemaName"], (string)dr["principalName"]));
				}
			}
		}

		private void LoadProps(SqlCommand cm)
		{
			var cnStrBuilder = new SqlConnectionStringBuilder(Connection);
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
			using (IDataReader dr = cm.ExecuteReader()) {
				if (dr.Read()) {
					SetPropString("COMPATIBILITY_LEVEL", dr["compatibility_level"]);
					SetPropString("COLLATE", dr["collation_name"]);
					SetPropOnOff("AUTO_CLOSE", dr["is_auto_close_on"]);
					SetPropOnOff("AUTO_SHRINK", dr["is_auto_shrink_on"]);
					if (dr["snapshot_isolation_state"] != DBNull.Value) {
						FindProp("ALLOW_SNAPSHOT_ISOLATION").Value = (byte) dr["snapshot_isolation_state"] == 0 ||
						                                             (byte) dr["snapshot_isolation_state"] == 2
							? "OFF"
							: "ON";
					}
					SetPropOnOff("READ_COMMITTED_SNAPSHOT", dr["is_read_committed_snapshot_on"]);
					SetPropString("RECOVERY", dr["recovery_model_desc"]);
					SetPropString("PAGE_VERIFY", dr["page_verify_option_desc"]);
					SetPropOnOff("AUTO_CREATE_STATISTICS", dr["is_auto_create_stats_on"]);
					SetPropOnOff("AUTO_UPDATE_STATISTICS", dr["is_auto_update_stats_on"]);
					SetPropOnOff("AUTO_UPDATE_STATISTICS_ASYNC", dr["is_auto_update_stats_async_on"]);
					SetPropOnOff("ANSI_NULL_DEFAULT", dr["is_ansi_null_default_on"]);
					SetPropOnOff("ANSI_NULLS", dr["is_ansi_nulls_on"]);
					SetPropOnOff("ANSI_PADDING", dr["is_ansi_padding_on"]);
					SetPropOnOff("ANSI_WARNINGS", dr["is_ansi_warnings_on"]);
					SetPropOnOff("ARITHABORT", dr["is_arithabort_on"]);
					SetPropOnOff("CONCAT_NULL_YIELDS_NULL", dr["is_concat_null_yields_null_on"]);
					SetPropOnOff("NUMERIC_ROUNDABORT", dr["is_numeric_roundabort_on"]);
					SetPropOnOff("QUOTED_IDENTIFIER", dr["is_quoted_identifier_on"]);
					SetPropOnOff("RECURSIVE_TRIGGERS", dr["is_recursive_triggers_on"]);
					SetPropOnOff("CURSOR_CLOSE_ON_COMMIT", dr["is_cursor_close_on_commit_on"]);
					if (dr["is_local_cursor_default"] != DBNull.Value) {
						FindProp("CURSOR_DEFAULT").Value = (bool) dr["is_local_cursor_default"] ? "LOCAL" : "GLOBAL";
					}
					SetPropOnOff("TRUSTWORTHY", dr["is_trustworthy_on"]);
					SetPropOnOff("DB_CHAINING", dr["is_db_chaining_on"]);
					if (dr["is_parameterization_forced"] != DBNull.Value) {
						FindProp("PARAMETERIZATION").Value = (bool) dr["is_parameterization_forced"] ? "FORCED" : "SIMPLE";
					}
					SetPropOnOff("DATE_CORRELATION_OPTIMIZATION", dr["is_date_correlation_on"]);
				}
			}
		}
#endregion

		public DatabaseDiff Compare(Database db) {
			var diff = new DatabaseDiff();
			diff.Db = db;

			//compare database properties           
			foreach (DbProp p in from p in Props
				let p2 = db.FindProp(p.Name)
				where p.Script() != p2.Script()
				select p) {
				diff.PropsChanged.Add(p);
			}

			//get tables added and changed
			foreach (List<Table> tables in new[] {Tables, TableTypes}) {
				foreach (Table t in tables) {
					Table t2 = db.FindTable(t.Name, t.Owner, t.IsType);
					if (t2 == null) {
						diff.TablesAdded.Add(t);
					} else {
						//compare mutual tables
						TableDiff tDiff = t.Compare(t2);
						if (tDiff.IsDiff) {
							if (t.IsType) { // types cannot be altered...
								diff.TableTypesDiff.Add(t);
							} else {
								diff.TablesDiff.Add(tDiff);
							}
						}
					}
				}
			}
			//get deleted tables
			foreach (Table t in db.Tables.Concat(db.TableTypes).Where(t => FindTable(t.Name, t.Owner, t.IsType) == null))
			{
				diff.TablesDeleted.Add(t);
			}

			//get procs added and changed
			foreach (Routine r in Routines) {
				Routine r2 = db.FindRoutine(r.Name, r.Schema);
				if (r2 == null) {
					diff.RoutinesAdded.Add(r);
				} else {
					//compare mutual procs
					if (r.Text.Trim() != r2.Text.Trim()) {
						diff.RoutinesDiff.Add(r);
					}
				}
			}
			//get procs deleted
			foreach (Routine r in db.Routines.Where(r => FindRoutine(r.Name, r.Schema) == null)) {
				diff.RoutinesDeleted.Add(r);
			}

			//get added and compare mutual foreign keys
			foreach (ForeignKey fk in ForeignKeys) {
				ForeignKey fk2 = db.FindForeignKey(fk.Name);
				if (fk2 == null) {
					diff.ForeignKeysAdded.Add(fk);
				} else {
					if (fk.ScriptCreate() != fk2.ScriptCreate()) {
						diff.ForeignKeysDiff.Add(fk);
					}
				}
			}
			//get deleted foreign keys
			foreach (ForeignKey fk in db.ForeignKeys.Where(fk => FindForeignKey(fk.Name) == null)) {
				diff.ForeignKeysDeleted.Add(fk);
			}


			//get added and compare mutual assemblies
			foreach (SqlAssembly a in Assemblies) {
				SqlAssembly a2 = db.FindAssembly(a.Name);
				if (a2 == null) {
					diff.AssembliesAdded.Add(a);
				} else {
					if (a.ScriptCreate(this) != a2.ScriptCreate(db)) {
						diff.AssembliesDiff.Add(a);
					}
				}
			}
			//get deleted assemblies
			foreach (SqlAssembly a in db.Assemblies.Where(a => FindAssembly(a.Name) == null)) {
				diff.AssembliesDeleted.Add(a);
			}


			//get added and compare mutual users
			foreach (SqlUser u in Users) {
				SqlUser u2 = db.FindUser(u.Name);
				if (u2 == null) {
					diff.UsersAdded.Add(u);
				} else {
					if (u.ScriptCreate(this) != u2.ScriptCreate(db)) {
						diff.UsersDiff.Add(u);
					}
				}
			}
			//get deleted users
			foreach (SqlUser u in db.Users.Where(u => FindUser(u.Name) == null)) {
				diff.UsersDeleted.Add(u);
			}

			//get added and compare view indexes
			foreach (Constraint c in ViewIndexes) {
				Constraint c2 = db.FindViewIndex(c.Name);
				if (c2 == null) {
					diff.ViewIndexesAdded.Add(c);
				} else {
					if (c.Script() != c2.Script()) {
						diff.ViewIndexesDiff.Add(c);
					}
				}
			}
			//get deleted view indexes
			foreach (Constraint c in db.ViewIndexes.Where(c => FindViewIndex(c.Name) == null)) {
				diff.ViewIndexesDeleted.Add(c);
			}

			//get added and compare synonyms
			foreach (Synonym s in Synonyms)
			{
				Synonym s2 = db.FindSynonym(s.Name, s.Schema);
				if (s2 == null)
				{
					diff.SynonymsAdded.Add(s);
				}
				else
				{
					if (s.BaseObjectName != s2.BaseObjectName)
					{
						diff.SynonymsDiff.Add(s);
					}
				}
			}
			//get deleted synonyms
			foreach (Synonym s in db.Synonyms.Where(s => FindSynonym(s.Name, s.Schema) == null))
			{
				diff.SynonymsDeleted.Add(s);
			}

			return diff;
		}

		public string ScriptCreate() {
			var text = new StringBuilder();

			text.AppendFormat("CREATE DATABASE {0}", Name);
			text.AppendLine();
			text.AppendLine("GO");
			text.AppendFormat("USE {0}", Name);
			text.AppendLine();
			text.AppendLine("GO");
			text.AppendLine();

			if (Props.Count > 0) {
				text.Append(ScriptPropList(Props));
				text.AppendLine("GO");
				text.AppendLine();
			}

			if (Schemas.Count > 0) {
				text.Append(ScriptSchemas(Schemas));
				text.AppendLine("GO");
				text.AppendLine();
			}

			foreach (Table t in Tables.Concat(TableTypes)) {
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
				text.AppendLine(r.ScriptCreate(this));
				text.AppendLine();
				text.AppendLine("GO");
			}

			foreach (SqlAssembly a in Assemblies) {
				text.AppendLine(a.ScriptCreate(this));
				text.AppendLine();
				text.AppendLine("GO");
			}

			foreach (SqlUser u in Users) {
				text.AppendLine(u.ScriptCreate(this));
				text.AppendLine();
				text.AppendLine("GO");
			}

			foreach (Constraint c in ViewIndexes) {
				text.AppendLine(c.Script());
				text.AppendLine();
				text.AppendLine("GO");
			}

			foreach (Synonym s in Synonyms)
			{
				text.AppendLine(s.ScriptCreate());
				text.AppendLine();
				text.AppendLine("GO");
			}

			return text.ToString();
		}

		#region Script

		public void ScriptToDir(string tableHint = null) {
			if (Directory.Exists(Dir)) {
				// delete the existing script files
				foreach (
					string f in
						dirs.Where(dir => Directory.Exists(Dir + "/" + dir)).SelectMany(dir => Directory.GetFiles(Dir + "/" + dir))) {
					File.Delete(f);
				}
			}
			// create dir tree
			foreach (string dir in dirs.Where(dir => !Directory.Exists(Dir + "/" + dir))) {
				Directory.CreateDirectory(Dir + "/" + dir);
			}

			var text = new StringBuilder();
			text.Append(ScriptPropList(Props));
			text.AppendLine("GO");
			text.AppendLine();
			File.WriteAllText(string.Format("{0}/props.sql", Dir),
				text.ToString());

			if (Schemas.Count > 0) {
				text = new StringBuilder();
				text.Append(ScriptSchemas(Schemas));
				text.AppendLine("GO");
				text.AppendLine();
				File.WriteAllText(string.Format("{0}/schemas.sql", Dir),
					text.ToString());
			}

			foreach (Table t in Tables.Concat(TableTypes)) {
				File.WriteAllText(
					string.Format("{0}/tables/{1}.sql", Dir, MakeFileName(t)),
					t.ScriptCreate() + "\r\nGO\r\n"
					);
			}

			foreach (ForeignKey fk in ForeignKeys) {
				File.AppendAllText(
					string.Format("{0}/foreign_keys/{1}.sql", Dir, MakeFileName(fk.Table)),
					fk.ScriptCreate() + "\r\nGO\r\n"
					);
			}

			foreach (Routine r in Routines) {
				File.WriteAllText(
					string.Format("{0}/{1}/{2}.sql", Dir, r.RoutineType.ToString().ToLower() + "s", MakeFileName(r)),
					r.ScriptCreate(this) + "\r\nGO\r\n"
					);
			}

			foreach (Constraint c in ViewIndexes) {
				File.WriteAllText(
					string.Format("{0}/{1}/{2}.sql", Dir, "views", MakeFileName(c.Name)),
					c.Script() + "\r\nGO\r\n"
					);
			}

			foreach (SqlAssembly a in Assemblies) {
				File.WriteAllText(
					string.Format("{0}/{1}/{2}.sql", Dir, "assemblies", MakeFileName(a.Name)),
					a.ScriptCreate(this) + "\r\nGO\r\n"
					);
			}

			foreach (SqlUser u in Users) {
				File.WriteAllText(
					string.Format("{0}/{1}/{2}.sql", Dir, "users", MakeFileName(u.Name)),
					u.ScriptCreate(this) + "\r\nGO\r\n"
					);
			}

			foreach (Synonym s in Synonyms)
			{
				File.WriteAllText(
					string.Format("{0}/{1}/{2}.sql", Dir, "synonyms", MakeFileName(s)),
					s.ScriptCreate() + "\r\nGO\r\n"
					);
			}


			ExportData(tableHint);
		}

		private static string MakeFileName(Routine r) {
			return MakeFileName(r.Schema, r.Name);
		}

		private static string MakeFileName(Table t) {
			return (t.IsType ? "TYPE_" : string.Empty) + MakeFileName(t.Owner, t.Name);
		}

		private static string MakeFileName(Synonym s)
		{
			return MakeFileName(s.Schema, s.Name);
		}

		private static string MakeFileName(string value) {
			return MakeFileName("dbo", value);
		}

		private static string MakeFileName(string schema, string name) {
			// Dont' include schema name for objects in the dbo schema.
			// This maintains backward compatability for those who use
			// SchemaZen to keep their schemas under version control.
			return schema.ToLower() == "dbo" ? name : string.Format("{0}.{1}", schema, name);
		}

		public void ExportData(string tableHint = null) {
			string dataDir = Dir + "/data";
			if (!Directory.Exists(dataDir)) {
				Directory.CreateDirectory(dataDir);
			}
			foreach (Table t in DataTables) {
				StreamWriter sw = File.CreateText(dataDir + "/" + MakeFileName(t) + ".tsv");
				t.ExportData(Connection, sw, tableHint);
				sw.Flush();
				sw.Close();
			}
		}

		public static string ScriptPropList(IList<DbProp> props) {
			var text = new StringBuilder();

			text.AppendLine("DECLARE @DB VARCHAR(255)");
			text.AppendLine("SET @DB = DB_NAME()");
			foreach (string p in props.Select(p => p.Script()).Where(p => !string.IsNullOrEmpty(p))) {
				text.AppendLine(p);
			}
			return text.ToString();
		}

		public static string ScriptSchemas(IList<Schema> schemas) {
			var text = new StringBuilder();
			foreach (Schema s in schemas) {
				string schemaName = s.Name.Replace("'", "''");
				string owner = s.Owner.Replace("'", "''");
				text.AppendFormat(@"
if not exists(select s.schema_id from sys.schemas s where s.name = '{0}') 
	and exists(select p.principal_id from sys.database_principals p where p.name = '{1}') begin
	exec sp_executesql N'create schema [{0}] authorization [{1}]'
end
", schemaName, owner);
			}
			return text.ToString();
		}

		#endregion

		#region Create

		public void ImportData() {
			string dataDir = Dir + "\\data";
			if (!Directory.Exists(dataDir)) {
				return;
			}

			foreach (string f in Directory.GetFiles(dataDir)) {
				var fi = new FileInfo(f);
				string schema = "dbo";
				string table = Path.GetFileNameWithoutExtension(fi.Name);
				if (table.Contains(".")) {
					schema = fi.Name.Split('.')[0];
					table = fi.Name.Split('.')[1];
				}
				Table t = FindTable(table, schema);
				if (t == null) {
					continue;
				}
				try {
					t.ImportData(Connection, File.ReadAllText(fi.FullName));
				} catch (DataException ex) {
					throw new DataFileException(ex.Message, fi.FullName, ex.LineNumber);
				} catch (SqlBatchException ex) {
					throw new DataFileException(ex.Message, fi.FullName, ex.LineNumber);
				} catch (Exception ex) {
					throw new DataFileException(ex.Message, fi.FullName, -1);
				}
			}
		}

		public void CreateFromDir(bool overwrite) {
			if (DBHelper.DbExists(Connection)) {
				DBHelper.DropDb(Connection);
			}

			Console.WriteLine("Creating database...");
			//create database
			DBHelper.CreateDb(Connection);

			//run scripts
			if (File.Exists(Dir + "/props.sql")) {
				try {
					DBHelper.ExecBatchSql(Connection, File.ReadAllText(Dir + "/props.sql"));
				} catch (SqlBatchException ex) {
					throw new SqlFileException(Dir + "/props.sql", ex);
				}

				// COLLATE can cause connection to be reset
				// so clear the pool so we get a new connection
				DBHelper.ClearPool(Connection);
			}

			if (File.Exists(Dir + "/schemas.sql")) {
				try {
					DBHelper.ExecBatchSql(Connection, File.ReadAllText(Dir + "/schemas.sql"));
				} catch (SqlBatchException ex) {
					throw new SqlFileException(Dir + "/schemas.sql", ex);
				}
			}

			Console.WriteLine("Creating database objects...");
			// create db objects

			// resolve dependencies by trying over and over
			// if the number of failures stops decreasing then give up
			List<string> scripts = GetScripts();
			var errors = new List<SqlFileException>();
			int prevCount = -1;
			while (scripts.Count > 0 && (prevCount == -1 || errors.Count < prevCount)) {
				if (errors.Count > 0) {
					prevCount = errors.Count;
					Console.WriteLine(
						"{0} errors occurred, retrying...", errors.Count);
				}
				errors.Clear();
				foreach (string f in scripts.ToArray()) {
					try {
						DBHelper.ExecBatchSql(Connection, File.ReadAllText(f));
						scripts.Remove(f);
					} catch (SqlBatchException ex) {
						errors.Add(new SqlFileException(f, ex));
						//Console.WriteLine("Error occurred in {0}: {1}", f, ex);
					}
				}
			}
			if (!errors.Any() && prevCount > 0)
				Console.WriteLine("All errors resolved, were probably dependency issues...");
			Console.WriteLine();

			Load(); // load the schema first so we can import data
			Console.WriteLine("Importing data...");
			ImportData(); // load data

			Console.WriteLine("Data imported successfully.");
			if (Directory.Exists(Dir + "/after_data")) {
				foreach (string f in Directory.GetFiles(Dir + "/after_data", "*.sql")) {
					try {
						DBHelper.ExecBatchSql(Connection, File.ReadAllText(f));
					} catch (SqlBatchException ex) {
						errors.Add(new SqlFileException(f, ex));
					}
				}
			}

			Console.WriteLine("Adding foreign key constraints...");
			// foreign keys
			if (Directory.Exists(Dir + "/foreign_keys")) {
				foreach (string f in Directory.GetFiles(Dir + "/foreign_keys", "*.sql")) {
					try {
						DBHelper.ExecBatchSql(Connection, File.ReadAllText(f));
					} catch (SqlBatchException ex) {
						//throw new SqlFileException(f, ex);
						errors.Add(new SqlFileException(f, ex));
					}
				}
			}
			if (errors.Count > 0) {
				var ex = new BatchSqlFileException();
				ex.Exceptions = errors;
				throw ex;
			}
		}

		private List<string> GetScripts() {
			var scripts = new List<string>();
			foreach (
				string dirPath in dirs.Where(dir => dir != "foreign_keys").Select(dir => Dir + "/" + dir).Where(Directory.Exists)) {
				scripts.AddRange(Directory.GetFiles(dirPath, "*.sql"));
			}
			return scripts;
		}

		public void ExecCreate(bool dropIfExists) {
			var conStr = new SqlConnectionStringBuilder(Connection);
			string dbName = conStr.InitialCatalog;
			conStr.InitialCatalog = "master";
			if (DBHelper.DbExists(Connection)) {
				if (dropIfExists) {
					DBHelper.DropDb(Connection);
				} else {
					throw new ApplicationException(string.Format("Database {0} {1} already exists.",
						conStr.DataSource, dbName));
				}
			}
			DBHelper.ExecBatchSql(conStr.ToString(), ScriptCreate());
		}

		#endregion
	}

	public class DatabaseDiff {
		public List<SqlAssembly> AssembliesAdded = new List<SqlAssembly>();
		public List<SqlAssembly> AssembliesDeleted = new List<SqlAssembly>();
		public List<SqlAssembly> AssembliesDiff = new List<SqlAssembly>();
		public Database Db;
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

		public bool IsDiff {
			get {
				return PropsChanged.Count > 0
				       || TablesAdded.Count > 0
				       || TablesDiff.Count > 0
					   || TableTypesDiff.Count > 0
				       || TablesDeleted.Count > 0
				       || RoutinesAdded.Count > 0
				       || RoutinesDiff.Count > 0
				       || RoutinesDeleted.Count > 0
				       || ForeignKeysAdded.Count > 0
				       || ForeignKeysDiff.Count > 0
				       || ForeignKeysDeleted.Count > 0
				       || AssembliesAdded.Count > 0
				       || AssembliesDiff.Count > 0
				       || AssembliesDeleted.Count > 0
				       || UsersAdded.Count > 0
				       || UsersDiff.Count > 0
				       || UsersDeleted.Count > 0
				       || ViewIndexesAdded.Count > 0
				       || ViewIndexesDiff.Count > 0
				       || ViewIndexesDeleted.Count > 0
					   || SynonymsAdded.Count > 0
					   || SynonymsDiff.Count > 0
					   || SynonymsDeleted.Count > 0;
			}
		}
		
		private static string Summarize(bool includeNames, List<string> changes, string caption) {
			if (changes.Count == 0) return string.Empty;
			return changes.Count.ToString() + "x " + caption + (includeNames ? ("\r\n\t" + string.Join("\r\n\t", changes.ToArray())) : string.Empty) + "\r\n";
		}
		
		public string SummarizeChanges(bool includeNames) {
			var sb = new StringBuilder();
			sb.Append(Summarize(includeNames, AssembliesAdded.Select(o => o.Name).ToList(), "assemblies in source but not in target"));
			sb.Append(Summarize(includeNames, AssembliesDeleted.Select(o => o.Name).ToList(), "assemblies not in source but in target"));
			sb.Append(Summarize(includeNames, AssembliesDiff.Select(o => o.Name).ToList(), "assemblies altered"));
			sb.Append(Summarize(includeNames, ForeignKeysAdded.Select(o => o.Name).ToList(), "foreign keys in source but not in target"));
			sb.Append(Summarize(includeNames, ForeignKeysDeleted.Select(o => o.Name).ToList(), "foreign keys not in source but in target"));
			sb.Append(Summarize(includeNames, ForeignKeysDiff.Select(o => o.Name).ToList(), "foreign keys altered"));
			sb.Append(Summarize(includeNames, PropsChanged.Select(o => o.Name).ToList(), "properties changed"));
			sb.Append(Summarize(includeNames, RoutinesAdded.Select(o => string.Format("{0} {1}.{2}", o.RoutineType.ToString(), o.Schema, o.Name)).ToList(), "routines in source but not in target"));
			sb.Append(Summarize(includeNames, RoutinesDeleted.Select(o => string.Format("{0} {1}.{2}", o.RoutineType.ToString(), o.Schema, o.Name)).ToList(), "routines not in source but in target"));
			sb.Append(Summarize(includeNames, RoutinesDiff.Select(o => string.Format("{0} {1}.{2}", o.RoutineType.ToString(), o.Schema, o.Name)).ToList(), "routines altered"));
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
			sb.Append(Summarize(includeNames, SynonymsAdded.Select(o => string.Format("{0}.{1}", o.Schema, o.Name)).ToList(), "synonyms in source but not in target"));
			sb.Append(Summarize(includeNames, SynonymsDeleted.Select(o => string.Format("{0}.{1}", o.Schema, o.Name)).ToList(), "synonyms not in source but in target"));
			sb.Append(Summarize(includeNames, SynonymsDiff.Select(o => string.Format("{0}.{1}", o.Schema, o.Name)).ToList(), "synonyms altered"));
			return sb.ToString();
		}

		public string Script() {
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
				foreach (ForeignKey fk in ForeignKeysDeleted) {
					text.AppendLine(fk.ScriptDrop());
				}
				//delete modified foreign keys
				foreach (ForeignKey fk in ForeignKeysDiff) {
					text.AppendLine(fk.ScriptDrop());
				}
				text.AppendLine("GO");
			}

			//add tables
			if (TablesAdded.Count > 0) {
				foreach (Table t in TablesAdded) {
					text.Append(t.ScriptCreate());
				}
				text.AppendLine("GO");
			}

			//modify tables
			if (TablesDiff.Count > 0) {
				foreach (TableDiff t in TablesDiff) {
					text.Append(t.Script());
				}
				text.AppendLine("GO");
			}
			// TODO: table types... need to drop and recreate... but will fail if anything references them...

			//delete tables
			if (TablesDeleted.Count > 0) {
				foreach (Table t in TablesDeleted) {
					text.AppendLine(t.ScriptDrop());
				}
				text.AppendLine("GO");
			}

			//add foreign keys
			if (ForeignKeysAdded.Count + ForeignKeysDiff.Count > 0) {
				foreach (ForeignKey fk in ForeignKeysAdded) {
					text.AppendLine(fk.ScriptCreate());
				}
				//add modified foreign keys
				foreach (ForeignKey fk in ForeignKeysDiff) {
					text.AppendLine(fk.ScriptCreate());
				}
				text.AppendLine("GO");
			}

			//add & delete procs, functions, & triggers
			foreach (Routine r in RoutinesAdded) {
				text.AppendLine(r.ScriptCreate(Db));
				text.AppendLine("GO");
			}
			foreach (Routine r in RoutinesDiff) {
				// script alter if possible, otherwise drop and (re)create
				try {
					text.AppendLine(r.ScriptAlter(Db));
					text.AppendLine("GO");
				} catch {
					text.AppendLine(r.ScriptDrop());
					text.AppendLine("GO");
					text.AppendLine(r.ScriptCreate(Db));
					text.AppendLine("GO");
				}
			}
			foreach (Routine r in RoutinesDeleted) {
				text.AppendLine(r.ScriptDrop());
				text.AppendLine("GO");
			}

			//add & delete synonyms
			foreach (Synonym s in SynonymsAdded)
			{
				text.AppendLine(s.ScriptCreate());
				text.AppendLine("GO");
			}
			foreach (Synonym s in SynonymsDiff)
			{
				text.AppendLine(s.ScriptDrop());
				text.AppendLine("GO");
				text.AppendLine(s.ScriptCreate());
				text.AppendLine("GO");
			}
			foreach (Synonym s in SynonymsDeleted)
			{
				text.AppendLine(s.ScriptDrop());
				text.AppendLine("GO");
			}

			return text.ToString();
		}
	}
}
