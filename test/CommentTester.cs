using System.IO;
using NUnit.Framework;
using SchemaZen.Library;
using SchemaZen.Library.Models;

namespace SchemaZen.Tests
{
    [TestFixture]
    class CommentTester {
        private Database _db;
        private string _comment;

        [SetUp]
        public void SetUp() {
            _db = new Database("TestScriptTableType");
            _db.Connection = ConfigHelper.TestDB.Replace("database=TESTDB", "database=" + _db.Name);
            _db.ExecCreate(true);
            _comment = Database.AutoGenerateComment;

            DBHelper.ExecSql(_db.Connection, TestStrings.SetupTable0Script);
            DBHelper.ExecSql(_db.Connection, TestStrings.SetupTable1Script);
            DBHelper.ExecSql(_db.Connection, TestStrings.SetupTableTypeScript);
            DBHelper.ExecSql(_db.Connection, TestStrings.SetupFKScript);
            DBHelper.ExecSql(_db.Connection, TestStrings.SetupFuncScript);
            DBHelper.ExecSql(_db.Connection, TestStrings.SetupProcScript);
            DBHelper.ExecSql(_db.Connection, TestStrings.SetupRoleScript);
            DBHelper.ExecSql(_db.Connection, TestStrings.SetupTrigScript);
            DBHelper.ExecSql(_db.Connection, TestStrings.SetupUserScript);
            DBHelper.ExecSql(_db.Connection, TestStrings.SetupViewScript);
        }

        [Test]
        public void TestFilesContainComment()
        {
            _db.Dir = _db.Name;
            _db.Load();
            _db.ScriptToDir();

            Assert.IsTrue(validateFirstLineIncludesComment(_db.Name +
                "\\table_types\\" + TestStrings.TableTypeFileName, _comment));
            Assert.IsTrue(validateFirstLineIncludesComment(_db.Name +
                "\\foreign_keys\\" + TestStrings.TestForeignKeyFileName, _comment));
            Assert.IsTrue(validateFirstLineIncludesComment(_db.Name +
                "\\functions\\" + TestStrings.TestFunctionFileName, _comment));
            Assert.IsTrue(validateFirstLineIncludesComment(_db.Name +
                "\\procedures\\" + TestStrings.TestProcedureFileName, _comment));
            Assert.IsTrue(validateFirstLineIncludesComment(_db.Name +
                "\\roles\\" + TestStrings.TestRoleFileName, _comment));
            Assert.IsTrue(validateFirstLineIncludesComment(_db.Name +
                "\\triggers\\" + TestStrings.TestTrigFileName, _comment));
            Assert.IsTrue(validateFirstLineIncludesComment(_db.Name +
                "\\tables\\" + TestStrings.TestTable0FileName, _comment));
            Assert.IsTrue(validateFirstLineIncludesComment(_db.Name +
                "\\users\\" + TestStrings.TestUserFileName, _comment));
            Assert.IsTrue(validateFirstLineIncludesComment(_db.Name +
                "\\views\\" + TestStrings.TestViewFileName, _comment));
            Assert.IsTrue(validateFirstLineIncludesComment(_db.Name + 
                "\\" + TestStrings.PropsFileName, _comment));
            Assert.IsTrue(validateFirstLineIncludesComment(_db.Name + 
                "\\" + TestStrings.SchemasFileName, _comment));
        }

        bool validateFirstLineIncludesComment(string filePath, string matchingStr) {
            string firstLine = File.ReadAllLines(filePath)[0];
            return matchingStr.Contains(firstLine);
        }
    }
}
