using System.IO;
using System.Runtime.InteropServices;
using NUnit.Framework;
using SchemaZen.Library;
using SchemaZen.Library.Models;

namespace SchemaZen.Tests
{
    class CommentTester {
        private Database db;

        [SetUp]
        public void SetUp() {
            db = new Database("TestScriptTableType");
            db.Connection = ConfigHelper.TestDB.Replace("database=TESTDB", "database=" + db.Name);
            db.ExecCreate(true);

            DBHelper.ExecSql(db.Connection, TestStrings.SetupTable0Script);
            DBHelper.ExecSql(db.Connection, TestStrings.SetupTable1Script);
            DBHelper.ExecSql(db.Connection, TestStrings.SetupTableTypeScript);
            DBHelper.ExecSql(db.Connection, TestStrings.SetupFKScript);
            DBHelper.ExecSql(db.Connection, TestStrings.SetupFuncScript);
            DBHelper.ExecSql(db.Connection, TestStrings.SetupProcScript);
            DBHelper.ExecSql(db.Connection, TestStrings.SetupRoleScript);
            DBHelper.ExecSql(db.Connection, TestStrings.SetupTrigScript);
            DBHelper.ExecSql(db.Connection, TestStrings.SetupUserScript);
            DBHelper.ExecSql(db.Connection, TestStrings.SetupViewScript);
        }

        [Test]
        public void TestScriptTableType()
        {
            db.Dir = db.Name;
            db.Load();
            db.ScriptToDir();

            Assert.IsTrue(File.Exists(db.Name + "\\table_types\\TYPE_TestTableType.sql"));
        }
    }
}
