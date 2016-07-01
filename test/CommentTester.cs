using System.IO;
using NUnit.Framework;
using SchemaZen.Library;
using SchemaZen.Library.Models;

namespace SchemaZen.Tests
{
    class CommentTester 
    {
        [Test]
        public void TestScriptTableType()
        {
            var db = new Database("TestScriptTableType");
            db.Connection = ConfigHelper.TestDB.Replace("database=TESTDB", "database=" + db.Name);
            db.ExecCreate(true);

            DBHelper.ExecSql(db.Connection, TestUtils.SetupTable0Script);
            DBHelper.ExecSql(db.Connection, TestUtils.SetupTable1Script);
            DBHelper.ExecSql(db.Connection, TestUtils.SetupTableTypeScript);
            DBHelper.ExecSql(db.Connection, TestUtils.SetupFKScript);
            DBHelper.ExecSql(db.Connection, TestUtils.SetupFuncScript);
            DBHelper.ExecSql(db.Connection, TestUtils.SetupProcScript);
            DBHelper.ExecSql(db.Connection, TestUtils.SetupRoleScript);
            DBHelper.ExecSql(db.Connection, TestUtils.SetupTrigScript);
            DBHelper.ExecSql(db.Connection, TestUtils.SetupUserScript);
            DBHelper.ExecSql(db.Connection, TestUtils.SetupViewScript);

            db.Dir = db.Name;
            db.Load();
            db.ScriptToDir();

            Assert.IsTrue(File.Exists(db.Name + "\\table_types\\TYPE_TestTableType.sql"));

        }
    }
}
