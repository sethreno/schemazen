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
            db2.Load(TestHelper.GetConnString("TEST_TEMP"));

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
                copy.Load(TestHelper.GetConnString("TEST_SOURCE"));
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
            source.Load(TestHelper.GetConnString("TEST_SOURCE"));

            Database copy = new Database("TEST_COPY");
            copy.Load(TestHelper.GetConnString("TEST_COPY"));

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

    }
}