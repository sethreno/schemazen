using System;
using System.Data.SqlClient;
using System.IO;
using console;
using model;
using NUnit.Framework;

namespace test {
	[TestFixture]
	internal class CreateTester {
		[Test]
		public void TestRun() {
			var db = new Database();
			db.Name = "CREATE_TEST";
			db.Dir = db.Name;
			db.ScriptToDir(true);

			var cnStr = new SqlConnectionStringBuilder(ConfigHelper.TestDB);
			cnStr.InitialCatalog = db.Name;
			DBHelper.DropDb(cnStr.ToString());

			var cmd = new Create();
			string[] args = {"create", db.Dir, "cn:" + cnStr};
			Assert.IsTrue(cmd.Parse(args));

			var consoleOut = new StringWriter();
			Console.SetOut(consoleOut);
			Assert.IsTrue(cmd.Run());

			Assert.IsTrue(consoleOut.ToString().Contains("Database created successfully"));

			Console.SetIn(new StringReader("n"));
			Assert.IsFalse(cmd.Run());
			Assert.IsTrue(consoleOut.ToString().Contains("already exists do you want to drop it"));
			Assert.IsTrue(consoleOut.ToString().Contains("create command cancelled"));

			Console.SetIn(new StringReader("y"));
			Assert.IsTrue(cmd.Run());
			Assert.IsTrue(consoleOut.ToString().Contains("already exists do you want to drop it"));
			Assert.IsTrue(consoleOut.ToString().Contains("Database created successfully"));
		}
	}
}