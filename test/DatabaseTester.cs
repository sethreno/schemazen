using model;
using System;
using NUnit.Framework;
using System.Data.SqlClient;
using System.IO;

namespace test {
    [TestFixture()]
    public class DatabaseTester {

        [Test()]
        public void TestScript() {            
            Database db = new Database("TEST_TEMP");
            Table t1 = new Table("dbo", "t1");
            t1.Columns.Add(new Column("col1", "int", false, null));
            t1.Columns.Add(new Column("col2", "int", false, null));
            t1.Constraints.Add(new Constraint("pk_t1", "PRIMARY KEY", "col1,col2"));
            t1.FindConstraint("pk_t1").Clustered = true;

            Table t2 = new Table("dbo", "t2");
            t2.Columns.Add(new Column("col1", "int", false, null));
            t2.Columns.Add(new Column("col2", "int", false, null));
            t2.Columns.Add(new Column("col3", "int", false, null));
            t2.Constraints.Add(new Constraint("pk_t2", "PRIMARY KEY", "col1"));
            t2.FindConstraint("pk_t2").Clustered = true;
            t2.Constraints.Add(new Constraint("IX_col3", "UNIQUE", "col3"));

            db.ForeignKeys.Add(new ForeignKey(t2, "fk_t2_t1", "col2,col3", t1, "col1,col2"));

            db.Tables.Add(t1);
            db.Tables.Add(t2);

            TestHelper.DropDb("TEST_TEMP");
            SqlConnection.ClearAllPools();
            TestHelper.ExecBatchSql(db.ScriptCreate(), "master");

            Database db2 = new Database();
			db2.Connection = TestHelper.GetConnString("TEST_TEMP");
			db2.Load();

            TestHelper.DropDb("TEST_TEMP");

            foreach (Table t in db.Tables) {
                Assert.IsNotNull(db2.FindTable(t.Name));
                Assert.IsFalse(db2.FindTable(t.Name).Compare(t).IsDiff);
            }
        }

        [Test()]
        public void TestCopy() {
            foreach (string script in Directory.GetFiles(ConfigHelper.TestSchemaDir)) {
                TestHelper.DropDb("TEST_SOURCE");
                TestHelper.DropDb("TEST_COPY");

                //create the db from sql script
                TestHelper.ExecSql("CREATE DATABASE TEST_SOURCE", "");
                TestHelper.ExecBatchSql(File.ReadAllText(script), "TEST_SOURCE");

                //load the model from newly created db and create a copy
                Database copy = new Database("TEST_COPY");
				copy.Connection = TestHelper.GetConnString("TEST_SOURCE");
                copy.Load();
                SqlConnection.ClearAllPools();
                TestHelper.ExecBatchSql(copy.ScriptCreate(), "master");

                //compare the dbs to make sure they are the same
                string cmd = string.Format("/c {0}\\SQLDBDiffConsole.exe {1} {2} {0}\\{3}", ConfigHelper.SqlDbDiffPath, "localhost\\SQLEXPRESS TEST_COPY   NULL NULL Y", "localhost\\SQLEXPRESS TEST_SOURCE NULL NULL Y", "SqlDbDiff.XML CompareResult.txt null");
                
                var proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = "cmd.exe";
                proc.StartInfo.Arguments = cmd;
                proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;                
                proc.Start();
                proc.WaitForExit();
                Assert.AreEqual("no difference", File.ReadAllLines("CompareResult.txt")[0]);
            }
        }

        [Test()]
        public void TestDiffScript() {
            TestHelper.DropDb("TEST_SOURCE");
            TestHelper.DropDb("TEST_COPY");

            //create the dbs from sql script
            string script = File.ReadAllText(ConfigHelper.TestSchemaDir + "\\BOP_QUOTE.sql");
            TestHelper.ExecSql("CREATE DATABASE TEST_SOURCE", "");
            TestHelper.ExecBatchSql(script, "TEST_SOURCE");

            script = File.ReadAllText(ConfigHelper.TestSchemaDir + "\\BOP_QUOTE_2.sql");
            TestHelper.ExecSql("CREATE DATABASE TEST_COPY", "");
            TestHelper.ExecBatchSql(script, "TEST_COPY");

            Database source = new Database("TEST_SOURCE");
			source.Connection = TestHelper.GetConnString("TEST_SOURCE");
            source.Load();

            Database copy = new Database("TEST_COPY");
			copy.Connection = TestHelper.GetConnString("TEST_COPY");
            copy.Load();

            //execute migration script to make SOURCE the same as COPY
            DatabaseDiff diff = copy.Compare(source);
            TestHelper.ExecBatchSql(diff.Script(), "TEST_SOURCE");

            //compare the dbs to make sure they are the same
            string cmd = string.Format("/c {0}\\SQLDBDiffConsole.exe {1} {2} {0}\\{3}", ConfigHelper.SqlDbDiffPath, "localhost\\SQLEXPRESS TEST_COPY   NULL NULL Y", "localhost\\SQLEXPRESS TEST_SOURCE NULL NULL Y", "SqlDbDiff.XML CompareResult.txt null");
            var proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = "cmd.exe";
            proc.StartInfo.Arguments = cmd;
            proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            proc.Start();
            proc.WaitForExit();

            Assert.AreEqual("no difference", File.ReadAllLines("CompareResult.txt")[0]);
        }

        [Test()]
        public void TestFindTableRegEx() {
            Database db = new Database();
            db.Tables.Add(new Table("dbo", "cmicDeductible"));
            db.Tables.Add(new Table("dbo", "cmicZipCode"));
            db.Tables.Add(new Table("dbo", "cmicState"));
            db.Tables.Add(new Table("dbo", "Policy"));
            db.Tables.Add(new Table("dbo", "Location"));
            db.Tables.Add(new Table("dbo", "Rate"));

            Assert.AreEqual(3, db.FindTablesRegEx("^cmic").Count);
            Assert.AreEqual(1, db.FindTablesRegEx("Location").Count);
        }

		[Test()]
		public void TestScriptToDir() {
			var policy = new Table("dbo", "Policy");
			policy.Columns.Add(new Column("id", "int", false, null));
			policy.Columns.Add(new Column("form", "tinyint", false, null));
			policy.Constraints.Add(new Constraint("PK_Policy", "PRIMARY KEY", "id"));
			policy.Columns.Items[0].Identity = new Identity(1, 1);

			var loc = new Table("dbo", "Location");
			loc.Columns.Add(new Column("id", "int", false, null));
			loc.Columns.Add(new Column("policyId", "int", false, null));
			loc.Columns.Add(new Column("storage", "bit", false, null));
			loc.Constraints.Add(new Constraint("PK_Location", "PRIMARY KEY", "id"));
			loc.Columns.Items[0].Identity = new Identity(1, 1);

			var formType = new Table("dbo", "FormType");
			formType.Columns.Add(new Column("code", "tinyint", false, null));
			formType.Columns.Add(new Column("desc", "varchar", 10, false, null));
			formType.Constraints.Add(new Constraint("PK_FormType", "PRIMARY KEY", "code"));
						
			var fk_policy_formType = new ForeignKey("FK_Policy_FormType");
			fk_policy_formType.Table = policy;
			fk_policy_formType.Columns.Add("form");
			fk_policy_formType.RefTable = formType;
			fk_policy_formType.RefColumns.Add("code");

			var fk_location_policy = new ForeignKey("FK_Location_Policy");
			fk_location_policy.Table = loc;
			fk_location_policy.Columns.Add("policyId");
			fk_location_policy.RefTable = policy;
			fk_location_policy.RefColumns.Add("id");
			fk_location_policy.OnDelete = "CASCADE";

			var db = new Database("ScriptTest");
			db.Tables.Add(policy);
			db.Tables.Add(formType);
			db.Tables.Add(loc);
			db.ForeignKeys.Add(fk_policy_formType);
			db.ForeignKeys.Add(fk_location_policy);

			db.Connection = "server=localhost\\SQLEXPRESS;"
				+ "database=ScriptTest;Trusted_Connection=yes;";
			db.ExecCreate(true);

			DBHelper.ExecSql(db.Connection,
				"  insert into formType ([code], [desc]) values (1, 'DP-1')\n"
				+ "insert into formType ([code], [desc]) values (2, 'DP-2')\n"
				+ "insert into formType ([code], [desc]) values (3, 'DP-3')");

			db.DataTables.Add(formType);
			db.Dir = db.Name;
			db.ScriptToDir(true);
			Assert.IsTrue(Directory.Exists(db.Name));
			Assert.IsTrue(Directory.Exists(db.Name + "\\data"));
			Assert.IsTrue(Directory.Exists(db.Name + "\\tables"));
			Assert.IsTrue(Directory.Exists(db.Name + "\\foreign_keys"));

			foreach(Table t in db.DataTables){
				Assert.IsTrue(File.Exists(db.Name + "\\data\\" + t.Name));
			}
			foreach(Table t in db.Tables){
				Assert.IsTrue(File.Exists(db.Name + "\\tables\\" + t.Name + ".sql"));
			}
			foreach(ForeignKey fk in db.ForeignKeys){
				Assert.IsTrue(File.Exists(db.Name + "\\foreign_keys\\" + fk.Table.Name + ".sql"));
			}			
		}
    }
}