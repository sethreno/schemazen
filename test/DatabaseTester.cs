using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SchemaZen.model;

namespace SchemaZen.test {
	[TestFixture]
	public class DatabaseTester {
		public static void TestCopySchema(string pathToSchemaScript) {
			TestHelper.DropDb("TEST_SOURCE");
			TestHelper.DropDb("TEST_COPY");

			//create the db from sql script
			TestHelper.ExecSql("CREATE DATABASE TEST_SOURCE", "");
			TestHelper.ExecBatchSql(File.ReadAllText(pathToSchemaScript), "TEST_SOURCE");

			//load the model from newly created db and create a copy
			var copy = new Database("TEST_COPY");
			copy.Connection = TestHelper.GetConnString("TEST_SOURCE");
			copy.Load();
			SqlConnection.ClearAllPools();
			var scripted = copy.ScriptCreate();
			TestHelper.ExecBatchSql(scripted, "master");

			//compare the dbs to make sure they are the same
			var source = new Database("TEST_SOURCE");
			source.Connection = TestHelper.GetConnString("TEST_SOURCE");
			source.Load();
			copy.Load();
			TestCompare(source, copy);
		}

		private static void TestCompare(Database source, Database copy) {
			//compare the dbs to make sure they are the same                        
			Assert.IsFalse(source.Compare(copy).IsDiff);

			// get a second opinion
			// if you ever find your license key
			/*
						var cmd = string.Format("/c {0}\\SQLDBDiffConsole.exe {1} {2} {0}\\{3}",
							ConfigHelper.SqlDbDiffPath,
							"localhost\\SQLEXPRESS " + copy.Name + " NULL NULL Y",
							"localhost\\SQLEXPRESS " + source.Name + " NULL NULL Y",
							"SqlDbDiff.XML CompareResult.txt null");

						Console.WriteLine(cmd);
						var proc = new Process();
						proc.StartInfo.FileName = "cmd.exe";
						proc.StartInfo.Arguments = cmd;
						proc.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
						proc.Start();
						proc.WaitForExit();
						Assert.AreEqual("no difference", File.ReadAllLines("CompareResult.txt")[0]);
			*/
		}

		[Test]
		public void TestBug13() {
			TestCopySchema(ConfigHelper.TestSchemaDir + "/SANDBOX3_GBL.SQL");
		}

		[Test]
		public void TestCollate() {
			var pathToSchema = ConfigHelper.TestSchemaDir + "/SANDBOX3_GBL.SQL";
			TestHelper.DropDb("TEST_SOURCE");

			//create the db from sql script
			TestHelper.ExecSql("CREATE DATABASE TEST_SOURCE", "");
			TestHelper.ExecBatchSql(File.ReadAllText(pathToSchema), "TEST_SOURCE");

			//load the model from newly created db and check collation
			var copy = new Database("TEST_COPY");
			copy.Connection = TestHelper.GetConnString("TEST_SOURCE");
			copy.Load();

			Assert.AreEqual("SQL_Latin1_General_CP1_CI_AS", copy.FindProp("COLLATE").Value);
		}

		[Test]
		public void TestCopy() {
			foreach (var script in Directory.GetFiles(ConfigHelper.TestSchemaDir)) {
				Console.WriteLine("Testing {0}", script);
				TestCopySchema(script);
			}
		}

		[Test]
		[Ignore("test won't work without license key for sqldbdiff")]
		public void TestDiffScript() {
			TestHelper.DropDb("TEST_SOURCE");
			TestHelper.DropDb("TEST_COPY");

			//create the dbs from sql script
			var script = File.ReadAllText(ConfigHelper.TestSchemaDir + "\\BOP_QUOTE.sql");
			TestHelper.ExecSql("CREATE DATABASE TEST_SOURCE", "");
			TestHelper.ExecBatchSql(script, "TEST_SOURCE");

			script = File.ReadAllText(ConfigHelper.TestSchemaDir + "\\BOP_QUOTE_2.sql");
			TestHelper.ExecSql("CREATE DATABASE TEST_COPY", "");
			TestHelper.ExecBatchSql(script, "TEST_COPY");

			var source = new Database("TEST_SOURCE");
			source.Connection = TestHelper.GetConnString("TEST_SOURCE");
			source.Load();

			var copy = new Database("TEST_COPY");
			copy.Connection = TestHelper.GetConnString("TEST_COPY");
			copy.Load();

			//execute migration script to make SOURCE the same as COPY
			var diff = copy.Compare(source);
			TestHelper.ExecBatchSql(diff.Script(), "TEST_SOURCE");

			//compare the dbs to make sure they are the same
			var cmd = string.Format("/c {0}\\SQLDBDiffConsole.exe {1} {2} {0}\\{3}", ConfigHelper.SqlDbDiffPath,
				"localhost\\SQLEXPRESS TEST_COPY   NULL NULL Y", "localhost\\SQLEXPRESS TEST_SOURCE NULL NULL Y",
				"SqlDbDiff.XML CompareResult.txt null");
			var proc = new Process();
			proc.StartInfo.FileName = "cmd.exe";
			proc.StartInfo.Arguments = cmd;
			proc.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
			proc.Start();
			proc.WaitForExit();

			Assert.AreEqual("no difference", File.ReadAllLines("CompareResult.txt")[0]);
		}

		[Test]
		public void TestFindTableRegEx() {
			var db = new Database();
			db.Tables.Add(new Table("dbo", "cmicDeductible"));
			db.Tables.Add(new Table("dbo", "cmicZipCode"));
			db.Tables.Add(new Table("dbo", "cmicState"));
			db.Tables.Add(new Table("dbo", "Policy"));
			db.Tables.Add(new Table("dbo", "Location"));
			db.Tables.Add(new Table("dbo", "Rate"));

			Assert.AreEqual(3, db.FindTablesRegEx("^cmic").Count);
			Assert.AreEqual(1, db.FindTablesRegEx("Location").Count);
		}

		[Test]
		public void TestScript() {
			var db = new Database("TEST_TEMP");
			var t1 = new Table("dbo", "t1");
			t1.Columns.Add(new Column("col1", "int", false, null) {Position = 1});
			t1.Columns.Add(new Column("col2", "int", false, null) {Position = 2});
			t1.Constraints.Add(new Constraint("pk_t1", "PRIMARY KEY", "col1,col2"));
			t1.FindConstraint("pk_t1").Clustered = true;

			var t2 = new Table("dbo", "t2");
			t2.Columns.Add(new Column("col1", "int", false, null) {Position = 1});
			t2.Columns.Add(new Column("col2", "int", false, null) {Position = 2});
			t2.Columns.Add(new Column("col3", "int", false, null) {Position = 3});
			t2.Constraints.Add(new Constraint("pk_t2", "PRIMARY KEY", "col1"));
			t2.FindConstraint("pk_t2").Clustered = true;
			t2.Constraints.Add(new Constraint("IX_col3", "UNIQUE", "col3"));

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
				Assert.IsNotNull(db2.FindTable(t.Name, t.Owner));
				Assert.IsFalse(db2.FindTable(t.Name, t.Owner).Compare(t).IsDiff);
			}
		}

        [Test]
        public void TestScriptTableType()
        {
            var setupSQL1 = @"
CREATE TYPE [dbo].[MyTableType] AS TABLE(
	[ID] [nvarchar](250) NULL
)

";

            var db = new Database("TestScriptTableType");

            db.Connection = ConfigHelper.TestDB.Replace("database=TESTDB", "database=" + db.Name);

            db.ExecCreate(true);

            DBHelper.ExecSql(db.Connection, setupSQL1);

            db.Dir = db.Name;
            db.Load();

            db.ScriptToDir();

            Assert.AreEqual(1, db.TableTypes.Count());
            Assert.AreEqual(250, db.TableTypes[0].Columns.Items[0].Length);
            Assert.AreEqual("MyTableType", db.TableTypes[0].Name);
            Assert.IsTrue(File.Exists(db.Name + "\\table_types\\TYPE_MyTableType.sql"));

        }


        [Test]
        public void TestScriptTableTypePrimaryKey()
        {
            var setupSQL1 = @"
CREATE TYPE [dbo].[MyTableType] AS TABLE(
	[ID] [int] NOT NULL,
	[Value] [varchar](50) NOT NULL,
	PRIMARY KEY CLUSTERED 
(
	[ID] ASC
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

            Assert.AreEqual(1, db.TableTypes.Count());
            Assert.AreEqual(1, db.TableTypes[0].PrimaryKey.Columns.Count);
            Assert.AreEqual("ID", db.TableTypes[0].PrimaryKey.Columns[0]);
            Assert.AreEqual(50, db.TableTypes[0].Columns.Items[1].Length);
            Assert.AreEqual("MyTableType", db.TableTypes[0].Name);
            Assert.IsTrue(File.Exists(db.Name + "\\table_types\\TYPE_MyTableType.sql"));

            Assert.IsTrue(File.ReadAllText(db.Name + "\\table_types\\TYPE_MyTableType.sql").Contains("PRIMARY KEY"));

        }

		[Test]
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
			Assert.IsTrue(scriptUp.ToLower().Contains("drop procedure [dbo].[test]"));
			Assert.IsTrue(scriptDown.ToLower().Contains("create procedure [dbo].[test]"));
		}

		[Test]
		public void TestScriptToDir() {
			var policy = new Table("dbo", "Policy");
			policy.Columns.Add(new Column("id", "int", false, null) {Position = 1});
			policy.Columns.Add(new Column("form", "tinyint", false, null) {Position = 2});
			policy.Constraints.Add(new Constraint("PK_Policy", "PRIMARY KEY", "id"));
			policy.Constraints[0].Clustered = true;
			policy.Constraints[0].Unique = true;
			policy.Columns.Items[0].Identity = new Identity(1, 1);

			var loc = new Table("dbo", "Location");
			loc.Columns.Add(new Column("id", "int", false, null) {Position = 1});
			loc.Columns.Add(new Column("policyId", "int", false, null) {Position = 2});
			loc.Columns.Add(new Column("storage", "bit", false, null) {Position = 3});
			loc.Constraints.Add(new Constraint("PK_Location", "PRIMARY KEY", "id"));
			loc.Constraints[0].Clustered = true;
			loc.Constraints[0].Unique = true;
			loc.Columns.Items[0].Identity = new Identity(1, 1);

			var formType = new Table("dbo", "FormType");
			formType.Columns.Add(new Column("code", "tinyint", false, null) {Position = 1});
			formType.Columns.Add(new Column("desc", "varchar", 10, false, null) {Position = 2});
			formType.Constraints.Add(new Constraint("PK_FormType", "PRIMARY KEY", "code"));
			formType.Constraints[0].Clustered = true;

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

			var db = new Database("ScriptToDirTest");
			db.Tables.Add(policy);
			db.Tables.Add(formType);
			db.Tables.Add(loc);
			db.ForeignKeys.Add(fk_policy_formType);
			db.ForeignKeys.Add(fk_location_policy);
			db.FindProp("COMPATIBILITY_LEVEL").Value = "120";
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
			db.Dir = db.Name;
			db.ScriptToDir();
			Assert.IsTrue(Directory.Exists(db.Name));
			Assert.IsTrue(Directory.Exists(db.Name + "\\data"));
			Assert.IsTrue(Directory.Exists(db.Name + "\\tables"));
			Assert.IsTrue(Directory.Exists(db.Name + "\\foreign_keys"));

			foreach (var t in db.DataTables) {
				Assert.IsTrue(File.Exists(db.Name + "\\data\\" + t.Name + ".tsv"));
			}
			foreach (var t in db.Tables) {
				Assert.IsTrue(File.Exists(db.Name + "\\tables\\" + t.Name + ".sql"));
			}
			foreach (var expected in db.ForeignKeys.Select(fk => db.Name + "\\foreign_keys\\" + fk.Table.Name + ".sql")) {
				Assert.IsTrue(File.Exists(expected), "File does not exist" + expected);
			}

			var copy = new Database("ScriptToDirTestCopy");
			copy.Dir = db.Dir;
			copy.Connection = ConfigHelper.TestDB.Replace("database=TESTDB", "database=" + copy.Name);
			copy.CreateFromDir(true);
			copy.Load();
			TestCompare(db, copy);
		}
	}
}
