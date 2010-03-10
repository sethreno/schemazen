using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace test {
    
    [TestFixture()]
    class CreateTester {

        [Test()]
        public void TestRun() {
            var db = new model.Database();
            db.Name ="CREATE_TEST";
            db.Dir = db.Name;
            db.ScriptToDir(true);
           
            var cnStr = new System.Data.SqlClient.SqlConnectionStringBuilder(ConfigHelper.TestDB);
            cnStr.InitialCatalog = db.Name;
            model.DBHelper.DropDb(cnStr.ToString());
            
            var cmd = new console.Create();
            string[] args = {"create", db.Dir, "cn:" + cnStr.ToString()};
            Assert.IsTrue(cmd.Parse(args));

            var consoleOut = new System.IO.StringWriter();
            Console.SetOut(consoleOut);                        
            Assert.IsTrue(cmd.Run());

            Assert.IsTrue(consoleOut.ToString().Contains("Database created successfully"));

            Console.SetIn(new System.IO.StringReader("n"));
            Assert.IsFalse(cmd.Run());
            Assert.IsTrue(consoleOut.ToString().Contains("already exists do you want to drop it"));
            Assert.IsTrue(consoleOut.ToString().Contains("create command cancelled"));

            Console.SetIn(new System.IO.StringReader("y"));
            Assert.IsTrue(cmd.Run());
            Assert.IsTrue(consoleOut.ToString().Contains("already exists do you want to drop it"));
            Assert.IsTrue(consoleOut.ToString().Contains("Database created successfully"));
        }
    }
}
