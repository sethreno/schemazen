using System.Data.SqlClient;
using SchemaZen.Library;
using SchemaZen.Library.Models;
using Test.Helpers;
using Xunit;

namespace SchemaZen.Tests;

[Collection("TestDb")]
public class DatabaseTester {
	private TestDbFixture _testDb;

	public DatabaseTester(TestDbFixture testDb) {
		_testDb = testDb;
	}

	[Fact]
	public void TestCopyTestSchemas() {
		// Regression tests databases scripted by other tools.
		// To add a new test script the entire database to a single file and
		// put it in the test_schemas directory.
		foreach (var script in Directory.GetFiles(ConfigHelper.TestSchemaDir)) {
			Console.WriteLine("Testing {0}", script);
			TestCopySchema(script);
		}
	}

	private static void TestCopySchema(string pathToSchemaScript) {
		TestHelper.DropDb("TEST_SOURCE");
		TestHelper.DropDb("TEST_COPY");

		//create the db from sql script
		TestHelper.ExecSql("CREATE DATABASE TEST_SOURCE", "");
		TestHelper.ExecBatchSql(File.ReadAllText(pathToSchemaScript), "TEST_SOURCE");
		SqlConnection.ClearAllPools();

		//load the model from newly created db and create a copy
		var copy = new Database("TEST_COPY");
		copy.Connection = TestHelper.GetConnString("TEST_SOURCE");
		copy.Load();
		SqlConnection.ClearAllPools();
		var scripted = copy.ScriptCreate();
		TestHelper.ExecBatchSql(scripted, "master");

		//compare the dbs to make sure they are the same
		var source = new Database("TEST_SOURCE") {
			Connection = TestHelper.GetConnString("TEST_SOURCE")
		};
		source.Load();
		copy.Load();
		TestCompare(source, copy);
	}

	private static void TestCompare(Database source, Database copy) {
		//compare the dbs to make sure they are the same                        
		Assert.False(source.Compare(copy).IsDiff);
	}

	[Fact]
	public void TestDescIndex() {
		TestHelper.DropDb("test");
		TestHelper.ExecSql("create database test", "");

		TestHelper.ExecSql(@"create table MyTable (Id int)", "test");
		TestHelper.ExecSql(@"create nonclustered index MyIndex on MyTable (Id desc)", "test");
		var db = new Database("test") {
			Connection = TestHelper.GetConnString("test")
		};
		db.Load();
		var result = db.ScriptCreate();
		Assert.Contains(
			"CREATE NONCLUSTERED INDEX [MyIndex] ON [dbo].[MyTable] ([Id] DESC)",
			result);

		TestHelper.DropDb("test");
	}

	[Fact]
	public void TestCollate() {
		var pathToSchema = ConfigHelper.TestSchemaDir + "/SANDBOX3_GBL.SQL";
		TestHelper.DropDb("TEST_SOURCE");

		//create the db from sql script
		TestHelper.ExecSql("CREATE DATABASE TEST_SOURCE", "");
		TestHelper.ExecBatchSql(File.ReadAllText(pathToSchema), "TEST_SOURCE");
		SqlConnection.ClearAllPools();

		//load the model from newly created db and check collation
		var copy = new Database("TEST_COPY");
		copy.Connection = TestHelper.GetConnString("TEST_SOURCE");
		copy.Load();

		Assert.Equal("SQL_Latin1_General_CP1_CI_AS", copy.FindProp("COLLATE").Value);
	}

	[Fact]
	public void TestTableIndexesWithFilter() {
		TestHelper.DropDb("TEST");
		TestHelper.ExecSql("CREATE DATABASE TEST", "");

		TestHelper.ExecSql(@"CREATE TABLE MyTable (Id int, EndDate datetime)", "TEST");
		TestHelper.ExecSql(
			@"CREATE NONCLUSTERED INDEX MyIndex ON MyTable (Id) WHERE (EndDate) IS NULL",
			"TEST");

		var db = new Database("TEST") {
			Connection = TestHelper.GetConnString("TEST")
		};
		db.Load();
		var result = db.ScriptCreate();
		TestHelper.DropDb("TEST");

		Assert.Contains(
			"CREATE NONCLUSTERED INDEX [MyIndex] ON [dbo].[MyTable] ([Id]) WHERE ([EndDate] IS NULL)",
			result);
	}

	[Fact]
	public void TestViewIndexes() {
		TestHelper.DropDb("TEST");
		TestHelper.ExecSql("CREATE DATABASE TEST", "");

		TestHelper.ExecSql(
			@"CREATE TABLE MyTable (Id int, Name nvarchar(250), EndDate datetime)", "TEST");
		TestHelper.ExecSql(
			@"CREATE VIEW dbo.MyView WITH SCHEMABINDING as SELECT t.Id, t.Name, t.EndDate from dbo.MyTable t",
			"TEST");
		TestHelper.ExecSql(@"CREATE UNIQUE CLUSTERED INDEX MyIndex ON MyView (Id, Name)",
			"TEST");

		var db = new Database("TEST") {
			Connection = TestHelper.GetConnString("TEST")
		};
		db.Load();
		var result = db.ScriptCreate();
		TestHelper.DropDb("TEST");

		Assert.Contains(
			"CREATE UNIQUE CLUSTERED INDEX [MyIndex] ON [dbo].[MyView] ([Id], [Name])",
			result);
	}

	[Fact]
	public void TestFindTableRegEx() {
		var db = CreateSampleDataForRegExTests();

		Assert.Equal(3, db.FindTablesRegEx("^cmic").Count);
		Assert.Equal(1, db.FindTablesRegEx("Location").Count);
	}

	[Fact]
	public void TestFindTableRegEx_ExcludeOnly() {
		var db = CreateSampleDataForRegExTests();

		Assert.Equal(3, db.FindTablesRegEx(null, "^cmic").Count);
		Assert.Equal(5, db.FindTablesRegEx(null, "Location").Count);
	}

	[Fact]
	public void TestFindTableRegEx_BothIncludeExclude() {
		var db = CreateSampleDataForRegExTests();

		Assert.Equal(2, db.FindTablesRegEx("^cmic", "Code$").Count);
		Assert.Equal(0, db.FindTablesRegEx("Location", "Location").Count);
	}

	private static Database CreateSampleDataForRegExTests() {
		var db = new Database();
		db.Tables.Add(new Table("dbo", "cmicDeductible"));
		db.Tables.Add(new Table("dbo", "cmicZipCode"));
		db.Tables.Add(new Table("dbo", "cmicState"));
		db.Tables.Add(new Table("dbo", "Policy"));
		db.Tables.Add(new Table("dbo", "Location"));
		db.Tables.Add(new Table("dbo", "Rate"));
		return db;
	}

	[Fact]
	public void TestScript() {
		var db = new Database("TEST_TEMP");
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

		TestHelper.DropDb("TEST_TEMP");
		SqlConnection.ClearAllPools();
		TestHelper.ExecBatchSql(db.ScriptCreate(), "master");

		var db2 = new Database();
		db2.Connection = TestHelper.GetConnString("TEST_TEMP");
		db2.Load();

		TestHelper.DropDb("TEST_TEMP");

		foreach (var t in db.Tables) {
			var copy = db2.FindTable(t.Name, t.Owner);
			Assert.NotNull(copy);
			Assert.False(copy.Compare(t).IsDiff);
		}
	}

	[Fact]
	public void TestScriptTableType() {
		var setupSQL1 = @"
CREATE TYPE [dbo].[MyTableType] AS TABLE(
[ID] [nvarchar](250) NULL,
[Value] [numeric](5, 1) NULL,
[LongNVarchar] [nvarchar](max) NULL
)

";

		var db = new Database("TestScriptTableType");

		db.Connection = ConfigHelper.TestDB.Replace("database=TESTDB", "database=" + db.Name);

		db.ExecCreate(true);

		DBHelper.ExecSql(db.Connection, setupSQL1);

		db.Dir = db.Name;
		db.Load();

		db.ScriptToDir();

		Assert.Equal(1, db.TableTypes.Count());
		Assert.Equal(250, db.TableTypes[0].Columns.Items[0].Length);
		Assert.Equal(1, db.TableTypes[0].Columns.Items[1].Scale);
		Assert.Equal(5, db.TableTypes[0].Columns.Items[1].Precision);
		Assert.Equal(-1,
			db.TableTypes[0].Columns.Items[2].Length); //nvarchar(max) is encoded as -1
		Assert.Equal("MyTableType", db.TableTypes[0].Name);
		Assert.True(File.Exists(db.Name + "/table_types/TYPE_MyTableType.sql"));
	}

	[Fact]
	public void TestScriptTableTypePrimaryKey() {
		var setupSQL1 = @"
CREATE TYPE [dbo].[MyTableType] AS TABLE(
[ID] [int] NOT NULL,
[Value] [varchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
[ID]
)
)

";

		var db = new Database("TestScriptTableTypePrimaryKey");

		db.Connection = ConfigHelper.TestDB.Replace("database=TESTDB", "database=" + db.Name);

		db.ExecCreate(true);

		DBHelper.ExecSql(db.Connection, setupSQL1);

		db.Dir = db.Name;
		db.Load();

		db.ScriptToDir();

		Assert.Equal(1, db.TableTypes.Count());
		Assert.Equal(1, db.TableTypes[0].PrimaryKey.Columns.Count);
		Assert.Equal("ID", db.TableTypes[0].PrimaryKey.Columns[0].ColumnName);
		Assert.Equal(50, db.TableTypes[0].Columns.Items[1].Length);
		Assert.Equal("MyTableType", db.TableTypes[0].Name);
		Assert.True(File.Exists(db.Name + "/table_types/TYPE_MyTableType.sql"));

		Assert.True(File.ReadAllText(db.Name + "/table_types/TYPE_MyTableType.sql")
			.Contains("PRIMARY KEY"));
	}

	[Fact]
	public void TestScriptTableTypeComputedColumn() {
		var setupSQL1 = @"
CREATE TYPE [dbo].[MyTableType] AS TABLE(
[Value1] [int] NOT NULL,
[Value2] [int] NOT NULL,
[ComputedValue] AS ([VALUE1]+[VALUE2])
)
";
		var db = new Database("TestScriptTableTypeComputedColumn");

		db.Connection = ConfigHelper.TestDB.Replace("database=TESTDB", "database=" + db.Name);

		db.ExecCreate(true);

		DBHelper.ExecSql(db.Connection, setupSQL1);

		db.Dir = db.Name;
		db.Load();

		db.ScriptToDir();

		Assert.Equal(1, db.TableTypes.Count());
		Assert.Equal(3, db.TableTypes[0].Columns.Items.Count());
		Assert.Equal("ComputedValue", db.TableTypes[0].Columns.Items[2].Name);
		Assert.Equal("([VALUE1]+[VALUE2])", db.TableTypes[0].Columns.Items[2].ComputedDefinition);
		Assert.Equal("MyTableType", db.TableTypes[0].Name);
		Assert.True(File.Exists(db.Name + "/table_types/TYPE_MyTableType.sql"));
	}

	[Fact]
	public void TestScriptTableTypeColumnCheckConstraint() {
		var setupSQL1 = @"
CREATE TYPE [dbo].[MyTableType] AS TABLE(
[ID] [nvarchar](250) NULL,
[Value] [numeric](5, 1) NULL CHECK([Value]>(0)),
[LongNVarchar] [nvarchar](max) NULL
)
";
		var db = new Database("TestScriptTableTypeColumnCheckConstraint");

		db.Connection = ConfigHelper.TestDB.Replace("database=TESTDB", "database=" + db.Name);

		db.ExecCreate(true);

		DBHelper.ExecSql(db.Connection, setupSQL1);

		db.Dir = db.Name;
		db.Load();

		db.ScriptToDir();

		Assert.Equal(1, db.TableTypes.Count());
		Assert.Equal(1, db.TableTypes[0].Constraints.Count());
		var constraint = db.TableTypes[0].Constraints.First();
		Assert.Equal("([Value]>(0))", constraint.CheckConstraintExpression);
		Assert.Equal("MyTableType", db.TableTypes[0].Name);
		Assert.True(File.Exists(db.Name + "/table_types/TYPE_MyTableType.sql"));
	}

	[Fact]
	public void TestScriptTableTypeColumnDefaultConstraint() {
		var setupSQL1 = @"
CREATE TYPE [dbo].[MyTableType] AS TABLE(
[ID] [nvarchar](250) NULL,
[Value] [numeric](5, 1) NULL DEFAULT 0,
[LongNVarchar] [nvarchar](max) NULL
)
";
		var db = new Database("TestScriptTableTypeColumnDefaultConstraint");

		db.Connection = ConfigHelper.TestDB.Replace("database=TESTDB", "database=" + db.Name);

		db.ExecCreate(true);

		DBHelper.ExecSql(db.Connection, setupSQL1);

		db.Dir = db.Name;
		db.Load();

		db.ScriptToDir();

		Assert.Equal(1, db.TableTypes.Count());
		Assert.NotNull(db.TableTypes[0].Columns.Items[1].Default);
		Assert.Equal(" DEFAULT ((0))", db.TableTypes[0].Columns.Items[1].Default.ScriptCreate());

		Assert.Equal("MyTableType", db.TableTypes[0].Name);
		Assert.True(File.Exists(db.Name + "/table_types/TYPE_MyTableType.sql"));
	}

	[Fact]
	public void TestScriptFKSameName() {
		var setupSQL = @"
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

		var db = new Database("TestScriptFKSameName");

		db.Connection = ConfigHelper.TestDB.Replace("database=TESTDB", "database=" + db.Name);

		db.ExecCreate(true);

		DBHelper.ExecSql(db.Connection, setupSQL);

		db.Dir = db.Name;
		db.Load();

		// Required in order to expose the exception
		db.ScriptToDir();

		Assert.Equal(2, db.ForeignKeys.Count());
		Assert.Equal(db.ForeignKeys[0].Name, db.ForeignKeys[1].Name);
		Assert.NotEqual(db.ForeignKeys[0].Table.Owner, db.ForeignKeys[1].Table.Owner);

		Assert.Equal("CASCADE", db.FindForeignKey("FKName", "dbo").OnUpdate);
		Assert.Equal("NO ACTION", db.FindForeignKey("FKName", "s2").OnUpdate);

		Assert.Equal("NO ACTION", db.FindForeignKey("FKName", "dbo").OnDelete);
		Assert.Equal("CASCADE", db.FindForeignKey("FKName", "s2").OnDelete);
	}

	[Fact]
	public void TestScriptViewInsteadOfTrigger() {
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

		var db = new Database("TestScriptViewInsteadOfTrigger");

		db.Connection = ConfigHelper.TestDB.Replace("database=TESTDB", "database=" + db.Name);

		db.ExecCreate(true);

		DBHelper.ExecSql(db.Connection, setupSQL1);
		DBHelper.ExecSql(db.Connection, setupSQL2);
		DBHelper.ExecSql(db.Connection, setupSQL3);

		db.Dir = db.Name;
		db.Load();

		// Required in order to expose the exception
		db.ScriptToDir();

		var triggers = db.Routines.Where(x => x.RoutineType == Routine.RoutineKind.Trigger)
			.ToList();

		Assert.Equal(1, triggers.Count());
		Assert.Equal("TR_v1", triggers[0].Name);
		Assert.True(File.Exists(db.Name + "/triggers/TR_v1.sql"));
	}

	[Fact]
	public void TestScriptTriggerWithNoSets() {
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

		var db = new Database("TestScriptTrigger");

		// Set these properties to the defaults so they are not scripted
		db.FindProp("QUOTED_IDENTIFIER").Value = "ON";
		db.FindProp("ANSI_NULLS").Value = "ON";

		db.Connection = ConfigHelper.TestDB.Replace("database=TESTDB", "database=" + db.Name);

		db.ExecCreate(true);

		DBHelper.ExecSql(db.Connection, setupSQL1);
		DBHelper.ExecSql(db.Connection, setupSQL2);
		DBHelper.ExecSql(db.Connection, setupSQL3);

		db.Dir = db.Name;
		db.Load();

		db.ScriptToDir();

		var script = File.ReadAllText(db.Name + "/triggers/TR_1.sql");

		Assert.DoesNotContain(
			"INSERTEDENABLE", script);
	}

	[Fact]
	public void TestScriptDeletedProc() {
		var source = new Database();
		source.Routines.Add(new Routine("dbo", "test", null));
		source.FindRoutine("test", "dbo").RoutineType = Routine.RoutineKind.Procedure;
		source.FindRoutine("test", "dbo").Text = @"
create procedure [dbo].[test]
as 
select * from Table1
";

		var target = new Database();
		var scriptUp = target.Compare(source).Script();
		var scriptDown = source.Compare(target).Script();
		Assert.True(scriptUp.ToLower().Contains("drop procedure [dbo].[test]"));
		Assert.True(scriptDown.ToLower().Contains("create procedure [dbo].[test]"));
	}

	[Fact]
	public void TestScriptToDir() {
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

		db.Connection = ConfigHelper.TestDB.Replace("database=TESTDB", "database=" + db.Name);
		db.ExecCreate(true);

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
		copy.Dir = db.Dir;
		copy.Connection =
			ConfigHelper.TestDB.Replace("database=TESTDB", "database=" + copy.Name);
		copy.CreateFromDir(true);
		copy.Load();
		TestCompare(db, copy);
	}

	[Fact]
	public void TestScriptToDirOnlyCreatesNecessaryFolders() {
		var db = new Database("TestEmptyDB");

		db.Connection = ConfigHelper.TestDB.Replace("database=TESTDB", "database=" + db.Name);

		db.ExecCreate(true);

		db.Dir = db.Name;
		db.Load();

		if (Directory.Exists(db.Dir)
		   ) // if the directory exists, delete it to make it a fair test
			Directory.Delete(db.Dir, true);

		db.ScriptToDir();

		Assert.Equal(0, db.Assemblies.Count);
		Assert.Equal(0, db.DataTables.Count);
		Assert.Equal(0, db.ForeignKeys.Count);
		Assert.Equal(0, db.Routines.Count);
		Assert.Equal(0, db.Schemas.Count);
		Assert.Equal(0, db.Synonyms.Count);
		Assert.Equal(0, db.Tables.Count);
		Assert.Equal(0, db.TableTypes.Count);
		Assert.Equal(0, db.Users.Count);
		Assert.Equal(0, db.ViewIndexes.Count);

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
