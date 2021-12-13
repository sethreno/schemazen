using SchemaZen.Library;
using SchemaZen.Library.Models;
using Test.Integration.Helpers;
using Xunit;
using Xunit.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Test.Integration;

[Trait("Category", "Integration")]
public class DatabaseTester {
	private readonly TestDbHelper _dbHelper;

	private readonly ILogger _logger;

	public DatabaseTester(ITestOutputHelper output, TestDbHelper dbHelper) {
		_logger = output.BuildLogger();
		_dbHelper = dbHelper;
	}

	[Fact]
	public async Task TestDescIndex() {
		await using var testDb = await _dbHelper.CreateTestDbAsync();

		await testDb.ExecSqlAsync(@"create table MyTable (Id int)");
		await testDb.ExecSqlAsync(@"create nonclustered index MyIndex on MyTable (Id desc)");
		var db = new Database("test") { Connection = testDb.GetConnString() };
		db.Load();
		var result = db.ScriptCreate();

		Assert.Contains(
			"CREATE NONCLUSTERED INDEX [MyIndex] ON [dbo].[MyTable] ([Id] DESC)",
			result);
	}

	[Fact]
	public async Task TestTableIndexesWithFilter() {
		await using var testDb = await _dbHelper.CreateTestDbAsync();

		await testDb.ExecSqlAsync(
			@"CREATE TABLE MyTable (Id int, EndDate datetime)");

		await testDb.ExecSqlAsync(
			@"CREATE NONCLUSTERED INDEX MyIndex ON MyTable (Id) WHERE (EndDate) IS NULL");

		var db = new Database("TEST") { Connection = testDb.GetConnString() };
		db.Load();
		var result = db.ScriptCreate();

		Assert.Contains(
			"CREATE NONCLUSTERED INDEX [MyIndex] ON [dbo].[MyTable] ([Id]) WHERE ([EndDate] IS NULL)",
			result);
	}

	[Fact]
	public async Task TestViewIndexes() {
		await using var testDb = await _dbHelper.CreateTestDbAsync();

		await testDb.ExecSqlAsync(
			@"CREATE TABLE MyTable (Id int, Name nvarchar(250), EndDate datetime)");

		await testDb.ExecSqlAsync(
			@"CREATE VIEW dbo.MyView WITH SCHEMABINDING as SELECT t.Id, t.Name, t.EndDate from dbo.MyTable t");

		await testDb.ExecSqlAsync(
			@"CREATE UNIQUE CLUSTERED INDEX MyIndex ON MyView (Id, Name)");


		var db = new Database("TEST") { Connection = testDb.GetConnString() };
		db.Load();
		var result = db.ScriptCreate();

		Assert.Contains(
			"CREATE UNIQUE CLUSTERED INDEX [MyIndex] ON [dbo].[MyView] ([Id], [Name])",
			result);
	}

	[Fact]
	public async Task TestScript() {
		var db = new Database(_dbHelper.MakeTestDbName());
		var t1 = new Table("dbo", "t1");
		t1.Columns.Add(new Column("col1", "int", false, null) { Position = 1 });
		t1.Columns.Add(new Column("col2", "int", false, null) { Position = 2 });
		t1.AddConstraint(new Constraint("pk_t1", "PRIMARY KEY", "col1,col2") {
			IndexType = "CLUSTERED"
		});

		var t2 = new Table("dbo", "t2");
		t2.Columns.Add(new Column("col1", "int", false, null) { Position = 1 });

		var col2 = new Column("col2", "int", false, null) { Position = 2 };
		col2.Default = new Default(t2, col2, "df_col2", "((0))", false);
		t2.Columns.Add(col2);

		t2.Columns.Add(new Column("col3", "int", false, null) { Position = 3 });
		t2.AddConstraint(new Constraint("pk_t2", "PRIMARY KEY", "col1") {
			IndexType = "CLUSTERED"
		});
		t2.AddConstraint(
			Constraint.CreateCheckedConstraint("ck_col2", true, false, "([col2]>(0))"));
		t2.AddConstraint(new Constraint("IX_col3", "UNIQUE", "col3") {
			IndexType = "NONCLUSTERED"
		});

		db.ForeignKeys.Add(new ForeignKey(t2, "fk_t2_t1", "col2,col3", t1, "col1,col2"));

		db.Tables.Add(t1);
		db.Tables.Add(t2);

		await using var testDb = _dbHelper.CreateTestDb(db);

		var db2 = new Database();
		db2.Connection = testDb.GetConnString();
		db2.Load();

		foreach (var t in db.Tables) {
			var copy = db2.FindTable(t.Name, t.Owner);
			Assert.NotNull(copy);
			Assert.False(copy.Compare(t).IsDiff);
		}
	}

	[Fact]
	public async Task TestScriptTableType() {
		var setupSQL1 = @"
CREATE TYPE [dbo].[MyTableType] AS TABLE(
[ID] [nvarchar](250) NULL,
[Value] [numeric](5, 1) NULL,
[LongNVarchar] [nvarchar](max) NULL
)

";
		await using var testDb = await _dbHelper.CreateTestDbAsync();
		await testDb.ExecSqlAsync(setupSQL1);
		var db = new Database("test") { Connection = testDb.GetConnString() };
		db.Load();

		Assert.Single(db.TableTypes);
		Assert.Equal(250, db.TableTypes[0].Columns.Items[0].Length);
		Assert.Equal(1, db.TableTypes[0].Columns.Items[1].Scale);
		Assert.Equal(5, db.TableTypes[0].Columns.Items[1].Precision);
		Assert.Equal(-1, db.TableTypes[0].Columns.Items[2].Length);
		Assert.Equal("MyTableType", db.TableTypes[0].Name);

		var result = db.ScriptCreate();
		Assert.Contains(
			"CREATE TYPE [dbo].[MyTableType] AS TABLE",
			result);
	}

	[Fact]
	public async Task TestScriptTableTypePrimaryKey() {
		var sql = @"
CREATE TYPE [dbo].[MyTableType] AS TABLE(
[ID] [int] NOT NULL,
[Value] [varchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
[ID]
)
)

";

		await using var testDb = await _dbHelper.CreateTestDbAsync();
		await testDb.ExecSqlAsync(sql);
		var db = new Database("test") { Connection = testDb.GetConnString() };
		db.Load();

		Assert.Single(db.TableTypes);
		Assert.Single(db.TableTypes[0].PrimaryKey.Columns);
		Assert.Equal("ID", db.TableTypes[0].PrimaryKey.Columns[0].ColumnName);
		Assert.Equal(50, db.TableTypes[0].Columns.Items[1].Length);
		Assert.Equal("MyTableType", db.TableTypes[0].Name);

		var result = db.ScriptCreate();
		Assert.Contains("PRIMARY KEY", result);
	}

	[Fact]
	public async Task TestScriptTableTypeComputedColumn() {
		var sql = @"
CREATE TYPE [dbo].[MyTableType] AS TABLE(
[Value1] [int] NOT NULL,
[Value2] [int] NOT NULL,
[ComputedValue] AS ([VALUE1]+[VALUE2])
)
";
		await using var testDb = await _dbHelper.CreateTestDbAsync();
		await testDb.ExecSqlAsync(sql);
		var db = new Database("test") { Connection = testDb.GetConnString() };
		db.Load();

		Assert.Single(db.TableTypes);
		Assert.Equal(3, db.TableTypes[0].Columns.Items.Count());
		Assert.Equal("ComputedValue", db.TableTypes[0].Columns.Items[2].Name);
		Assert.Equal("([VALUE1]+[VALUE2])", db.TableTypes[0].Columns.Items[2].ComputedDefinition);
		Assert.Equal("MyTableType", db.TableTypes[0].Name);
	}

	[Fact]
	public async Task TestScriptTableTypeColumnCheckConstraint() {
		var sql = @"
CREATE TYPE [dbo].[MyTableType] AS TABLE(
[ID] [nvarchar](250) NULL,
[Value] [numeric](5, 1) NULL CHECK([Value]>(0)),
[LongNVarchar] [nvarchar](max) NULL
)
";
		await using var testDb = await _dbHelper.CreateTestDbAsync();
		await testDb.ExecSqlAsync(sql);
		var db = new Database("test") { Connection = testDb.GetConnString() };
		db.Load();

		Assert.Single(db.TableTypes);
		Assert.Single(db.TableTypes[0].Constraints);
		var constraint = db.TableTypes[0].Constraints.First();
		Assert.Equal("([Value]>(0))", constraint.CheckConstraintExpression);
		Assert.Equal("MyTableType", db.TableTypes[0].Name);
	}

	[Fact]
	public async Task TestScriptTableTypeColumnDefaultConstraint() {
		var sql = @"
CREATE TYPE [dbo].[MyTableType] AS TABLE(
[ID] [nvarchar](250) NULL,
[Value] [numeric](5, 1) NULL DEFAULT 0,
[LongNVarchar] [nvarchar](max) NULL
)
";
		await using var testDb = await _dbHelper.CreateTestDbAsync();
		await testDb.ExecSqlAsync(sql);
		var db = new Database("test") { Connection = testDb.GetConnString() };
		db.Load();

		Assert.Single(db.TableTypes);
		Assert.NotNull(db.TableTypes[0].Columns.Items[1].Default);
		Assert.Equal(" DEFAULT ((0))", db.TableTypes[0].Columns.Items[1].Default.ScriptCreate());
		Assert.Equal("MyTableType", db.TableTypes[0].Name);
	}

	[Fact]
	public async Task TestScriptFKSameName() {
		var sql = @"
CREATE SCHEMA [s2] AUTHORIZATION [dbo]

CREATE TABLE [dbo].[t1a]
(
a INT NOT NULL, 
CONSTRAINT [PK_1a] PRIMARY KEY (a)
)

CREATE TABLE [dbo].[t1b]
(
a INT NOT NULL,
CONSTRAINT [FKName] FOREIGN KEY ([a]) REFERENCES [dbo].[t1a] ([a]) ON UPDATE CASCADE
)

CREATE TABLE [s2].[t2a]
(
a INT NOT NULL, 
CONSTRAINT [PK_2a] PRIMARY KEY (a)
)

CREATE TABLE [s2].[t2b]
(
a INT NOT NULL,
CONSTRAINT [FKName] FOREIGN KEY ([a]) REFERENCES [s2].[t2a] ([a]) ON DELETE CASCADE
)

";

		await using var testDb = await _dbHelper.CreateTestDbAsync();
		await testDb.ExecSqlAsync(sql);
		var db = new Database("test") { Connection = testDb.GetConnString() };
		db.Load();

		// Required in order to expose the exception
		db.ScriptCreate();

		Assert.Equal(2, db.ForeignKeys.Count());
		Assert.Equal(db.ForeignKeys[0].Name, db.ForeignKeys[1].Name);
		Assert.NotEqual(db.ForeignKeys[0].Table.Owner, db.ForeignKeys[1].Table.Owner);

		Assert.Equal("CASCADE", db.FindForeignKey("FKName", "dbo").OnUpdate);
		Assert.Equal("NO ACTION", db.FindForeignKey("FKName", "s2").OnUpdate);

		Assert.Equal("NO ACTION", db.FindForeignKey("FKName", "dbo").OnDelete);
		Assert.Equal("CASCADE", db.FindForeignKey("FKName", "s2").OnDelete);
	}

	[Fact]
	public async Task TestScriptViewInsteadOfTrigger() {
		var setupSQL1 = @"
CREATE TABLE [dbo].[t1]
(
a INT NOT NULL, 
CONSTRAINT [PK] PRIMARY KEY (a)
)
";
		var setupSQL2 = @"

CREATE VIEW [dbo].[v1] AS

SELECT * FROM t1

";
		var setupSQL3 = @"

CREATE TRIGGER [dbo].[TR_v1] ON [dbo].[v1] INSTEAD OF DELETE AS

DELETE FROM [dbo].[t1] FROM [dbo].[t1] INNER JOIN DELETED ON DELETED.a = [dbo].[t1].a

";

		await using var testDb = await _dbHelper.CreateTestDbAsync();
		await testDb.ExecSqlAsync(setupSQL1);
		await testDb.ExecSqlAsync(setupSQL2);
		await testDb.ExecSqlAsync(setupSQL3);
		var db = new Database("test") { Connection = testDb.GetConnString() };
		db.Load();

		// Required in order to expose the exception
		db.ScriptCreate();

		var triggers = db.Routines.Where(x => x.RoutineType == Routine.RoutineKind.Trigger)
			.ToList();

		Assert.Single(triggers);
		Assert.Equal("TR_v1", triggers[0].Name);
	}

	[Fact]
	public async Task TestScriptTriggerWithNoSets() {
		var setupSQL1 = @"
CREATE TABLE [dbo].[t1]
(
a INT NOT NULL, 
CONSTRAINT [PK] PRIMARY KEY (a)
)
";

		var setupSQL2 = @"
CREATE TABLE [dbo].[t2]
(
a INT NOT NULL
)
";

		var setupSQL3 = @"

CREATE TRIGGER [dbo].[TR_1] ON [dbo].[t1]  FOR UPDATE,INSERT
AS INSERT INTO [dbo].[t2](a) SELECT a FROM INSERTED";

		await using var testDb = await _dbHelper.CreateTestDbAsync();
		await testDb.ExecSqlAsync(setupSQL1);
		await testDb.ExecSqlAsync(setupSQL2);
		await testDb.ExecSqlAsync(setupSQL3);
		var db = new Database("test") { Connection = testDb.GetConnString() };
		db.Load();

		// Set these properties to the defaults so they are not scripted
		db.FindProp("QUOTED_IDENTIFIER").Value = "ON";
		db.FindProp("ANSI_NULLS").Value = "ON";

		var script = db.ScriptCreate();

		Assert.DoesNotContain(
			"INSERTEDENABLE", script);
	}

	[Fact]
	public async Task TestScriptToDir() {
		var policy = new Table("dbo", "Policy");
		policy.Columns.Add(new Column("id", "int", false, null) { Position = 1 });
		policy.Columns.Add(new Column("form", "tinyint", false, null) { Position = 2 });
		policy.AddConstraint(new Constraint("PK_Policy", "PRIMARY KEY", "id") {
			IndexType = "CLUSTERED",
			Unique = true
		});
		policy.Columns.Items[0].Identity = new Identity(1, 1);

		var loc = new Table("dbo", "Location");
		loc.Columns.Add(new Column("id", "int", false, null) { Position = 1 });
		loc.Columns.Add(new Column("policyId", "int", false, null) { Position = 2 });
		loc.Columns.Add(new Column("storage", "bit", false, null) { Position = 3 });
		loc.Columns.Add(new Column("category", "int", false, null) { Position = 4 });
		loc.AddConstraint(new Constraint("PK_Location", "PRIMARY KEY", "id") {
			IndexType = "CLUSTERED",
			Unique = true
		});
		loc.Columns.Items[0].Identity = new Identity(1, 1);

		var formType = new Table("dbo", "FormType");
		formType.Columns.Add(new Column("code", "tinyint", false, null) { Position = 1 });
		formType.Columns.Add(new Column("desc", "varchar", 10, false, null) { Position = 2 });
		formType.AddConstraint(new Constraint("PK_FormType", "PRIMARY KEY", "code") {
			IndexType = "CLUSTERED",
			Unique = true
		});
		formType.AddConstraint(
			Constraint.CreateCheckedConstraint("CK_FormType", false, false, "([code]<(5))"));

		var categoryType = new Table("dbo", "CategoryType");
		categoryType.Columns.Add(new Column("id", "int", false, null) { Position = 1 });
		categoryType.Columns.Add(new Column("Category", "varchar", 10, false, null)
			{ Position = 2 });
		categoryType.AddConstraint(new Constraint("PK_CategoryType", "PRIMARY KEY", "id") {
			IndexType = "CLUSTERED",
			Unique = true
		});

		var emptyTable = new Table("dbo", "EmptyTable");
		emptyTable.Columns.Add(new Column("code", "tinyint", false, null) { Position = 1 });
		emptyTable.AddConstraint(new Constraint("PK_EmptyTable", "PRIMARY KEY", "code") {
			IndexType = "CLUSTERED",
			Unique = true
		});

		var fk_policy_formType = new ForeignKey("FK_Policy_FormType");
		fk_policy_formType.Table = policy;
		fk_policy_formType.Columns.Add("form");
		fk_policy_formType.RefTable = formType;
		fk_policy_formType.RefColumns.Add("code");
		fk_policy_formType.OnUpdate = "NO ACTION";
		fk_policy_formType.OnDelete = "NO ACTION";

		var fk_location_policy = new ForeignKey("FK_Location_Policy");
		fk_location_policy.Table = loc;
		fk_location_policy.Columns.Add("policyId");
		fk_location_policy.RefTable = policy;
		fk_location_policy.RefColumns.Add("id");
		fk_location_policy.OnUpdate = "NO ACTION";
		fk_location_policy.OnDelete = "CASCADE";

		var fk_location_category = new ForeignKey("FK_Location_category");
		fk_location_category.Table = loc;
		fk_location_category.Columns.Add("category");
		fk_location_category.RefTable = categoryType;
		fk_location_category.RefColumns.Add("id");
		fk_location_category.OnUpdate = "NO ACTION";
		fk_location_category.OnDelete = "CASCADE";

		var tt_codedesc = new Table("dbo", "CodeDesc");
		tt_codedesc.IsType = true;
		tt_codedesc.Columns.Add(new Column("code", "tinyint", false, null) { Position = 1 });
		tt_codedesc.Columns.Add(new Column("desc", "varchar", 10, false, null)
			{ Position = 2 });
		tt_codedesc.AddConstraint(new Constraint("PK_CodeDesc", "PRIMARY KEY", "code") {
			IndexType = "NONCLUSTERED"
		});

		var db = new Database("ScriptToDirTest");
		db.Tables.Add(policy);
		db.Tables.Add(formType);
		db.Tables.Add(categoryType);
		db.Tables.Add(emptyTable);
		db.Tables.Add(loc);
		db.TableTypes.Add(tt_codedesc);
		db.ForeignKeys.Add(fk_policy_formType);
		db.ForeignKeys.Add(fk_location_policy);
		db.ForeignKeys.Add(fk_location_category);
		db.FindProp("COMPATIBILITY_LEVEL").Value = "110";
		db.FindProp("COLLATE").Value = "SQL_Latin1_General_CP1_CI_AS";
		db.FindProp("AUTO_CLOSE").Value = "OFF";
		db.FindProp("AUTO_SHRINK").Value = "ON";
		db.FindProp("ALLOW_SNAPSHOT_ISOLATION").Value = "ON";
		db.FindProp("READ_COMMITTED_SNAPSHOT").Value = "OFF";
		db.FindProp("RECOVERY").Value = "SIMPLE";
		db.FindProp("PAGE_VERIFY").Value = "CHECKSUM";
		db.FindProp("AUTO_CREATE_STATISTICS").Value = "ON";
		db.FindProp("AUTO_UPDATE_STATISTICS").Value = "ON";
		db.FindProp("AUTO_UPDATE_STATISTICS_ASYNC").Value = "ON";
		db.FindProp("ANSI_NULL_DEFAULT").Value = "ON";
		db.FindProp("ANSI_NULLS").Value = "ON";
		db.FindProp("ANSI_PADDING").Value = "ON";
		db.FindProp("ANSI_WARNINGS").Value = "ON";
		db.FindProp("ARITHABORT").Value = "ON";
		db.FindProp("CONCAT_NULL_YIELDS_NULL").Value = "ON";
		db.FindProp("NUMERIC_ROUNDABORT").Value = "ON";
		db.FindProp("QUOTED_IDENTIFIER").Value = "ON";
		db.FindProp("RECURSIVE_TRIGGERS").Value = "ON";
		db.FindProp("CURSOR_CLOSE_ON_COMMIT").Value = "ON";
		db.FindProp("CURSOR_DEFAULT").Value = "LOCAL";
		db.FindProp("TRUSTWORTHY").Value = "ON";
		db.FindProp("DB_CHAINING").Value = "ON";
		db.FindProp("PARAMETERIZATION").Value = "FORCED";
		db.FindProp("DATE_CORRELATION_OPTIMIZATION").Value = "ON";

		await _dbHelper.DropDbAsync(db.Name);
		await using var testDb = _dbHelper.CreateTestDb(db);
		db.Connection = testDb.GetConnString();

		DBHelper.ExecSql(db.Connection,
			"  insert into formType ([code], [desc]) values (1, 'DP-1')\n"
			+ "insert into formType ([code], [desc]) values (2, 'DP-2')\n"
			+ "insert into formType ([code], [desc]) values (3, 'DP-3')");

		db.DataTables.Add(formType);
		db.DataTables.Add(emptyTable);
		db.Dir = db.Name;

		if (Directory.Exists(db.Dir))
			Directory.Delete(db.Dir, true);

		db.ScriptToDir();
		Assert.True(Directory.Exists(db.Name));
		Assert.True(Directory.Exists(db.Name + "/data"));
		Assert.True(Directory.Exists(db.Name + "/tables"));
		Assert.True(Directory.Exists(db.Name + "/foreign_keys"));

		foreach (var t in db.DataTables)
			if (t.Name == "EmptyTable")
				Assert.False(File.Exists(db.Name + "/data/" + t.Name + ".tsv"));
			else
				Assert.True(File.Exists(db.Name + "/data/" + t.Name + ".tsv"));

		foreach (var t in db.Tables) {
			var tblFile = db.Name + "/tables/" + t.Name + ".sql";
			Assert.True(File.Exists(tblFile));

			// Test that the constraints are ordered in the file
			var script = File.ReadAllText(tblFile);
			var cindex = -1;

			foreach (var ckobject in t.Constraints.Where(c => c.Type != "CHECK")
				         .OrderBy(x => x.Name)) {
				var thisindex = script.IndexOf(ckobject.ScriptCreate());
				Assert.True(thisindex > cindex, "Constraints are not ordered.");

				cindex = thisindex;
			}
		}

		foreach (var t in db.TableTypes)
			Assert.True(File.Exists(db.Name + "/table_types/TYPE_" + t.Name + ".sql"));

		foreach (var expected in db.ForeignKeys.Select(fk =>
			         db.Name + "/foreign_keys/" + fk.Table.Name + ".sql"))
			Assert.True(File.Exists(expected), "File does not exist" + expected);

		// Test that the foreign keys are ordered in the file
		foreach (var t in db.Tables) {
			var fksFile = db.Name + "/foreign_keys/" + t.Name + ".sql";

			if (File.Exists(fksFile)) {
				var script = File.ReadAllText(fksFile);
				var fkindex = -1;

				foreach (var fkobject in db.ForeignKeys.Where(x => x.Table == t)
					         .OrderBy(x => x.Name)) {
					var thisindex = script.IndexOf(fkobject.ScriptCreate());
					Assert.True(thisindex > fkindex, "Foreign keys are not ordered.");

					fkindex = thisindex;
				}
			}
		}

		var copy = new Database("ScriptToDirTestCopy");
		await _dbHelper.DropDbAsync("ScriptToDirTestCopy");
		await using var testDb2 = _dbHelper.CreateTestDb(copy);
		copy.Dir = db.Dir;
		copy.Connection = testDb2.GetConnString();
		copy.CreateFromDir(true);
		copy.Load();

		Assert.False(db.Compare(copy).IsDiff);
	}

	[Fact]
	public async Task TestScriptToDirOnlyCreatesNecessaryFolders() {
		var db = new Database("TestEmptyDB");
		await _dbHelper.DropDbAsync(db.Name);
		await using var testDb = _dbHelper.CreateTestDb(db);

		db.Connection = testDb.GetConnString();
		db.Dir = db.Name;
		db.Load();

		if (Directory.Exists(db.Dir))
			Directory.Delete(db.Dir, true);

		db.ScriptToDir();

		Assert.Empty(db.Assemblies);
		Assert.Empty(db.DataTables);
		Assert.Empty(db.ForeignKeys);
		Assert.Empty(db.Routines);
		Assert.Empty(db.Schemas);
		Assert.Empty(db.Synonyms);
		Assert.Empty(db.Tables);
		Assert.Empty(db.TableTypes);
		Assert.Empty(db.Users);
		Assert.Empty(db.ViewIndexes);

		Assert.True(Directory.Exists(db.Name));
		Assert.True(File.Exists(db.Name + "/props.sql"));
		//Assert.IsFalse(File.Exists(db.Name + "/schemas.sql"));

		Assert.False(Directory.Exists(db.Name + "/assemblies"));
		Assert.False(Directory.Exists(db.Name + "/data"));
		Assert.False(Directory.Exists(db.Name + "/foreign_keys"));
		foreach (var routineType in Enum.GetNames(typeof(Routine.RoutineKind))) {
			var dir = routineType.ToLower() + "s";
			Assert.False(Directory.Exists(db.Name + "/" + dir));
		}

		Assert.False(Directory.Exists(db.Name + "/synonyms"));
		Assert.False(Directory.Exists(db.Name + "/tables"));
		Assert.False(Directory.Exists(db.Name + "/table_types"));
		Assert.False(Directory.Exists(db.Name + "/users"));
	}
}
