﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using NUnit.Framework;
using SchemaZen.helpers;
using SchemaZen.model;

namespace SchemaZen.test {
	[TestFixture]
	public class TableTester {
		private List<List<string>> TabDataToList(string data) {
			var lines = new List<List<string>>();
			foreach (var line in data.Split('\t')) {
				lines.Add(new List<string>());
				foreach (var field in line.Split('\t')) {
					lines[lines.Count - 1].Add(field);
				}
			}
			////remove the \r from the end of the last field of each line
			//foreach (List<string> line in lines) {
			//    if (line.Count == 0) continue;
			//    line[line.Count - 1] = line.Last.Remove(line.Last.Length - 1, 1);
			//}
			return lines;
		}

		[Test]
		public void TestCompare() {
			var t1 = new Table("dbo", "Test");
			var t2 = new Table("dbo", "Test");
			var diff = default(TableDiff);

			//test equal
			t1.Columns.Add(new Column("first", "varchar", 30, false, null));
			t2.Columns.Add(new Column("first", "varchar", 30, false, null));
			t1.Constraints.Add(new Constraint("PK_Test", "PRIMARY KEY", "first"));
			t2.Constraints.Add(new Constraint("PK_Test", "PRIMARY KEY", "first"));

			diff = t1.Compare(t2);
			Assert.IsNotNull(diff);
			Assert.IsFalse(diff.IsDiff);

			//test add
			t1.Columns.Add(new Column("second", "varchar", 30, false, null));
			diff = t1.Compare(t2);
			Assert.IsTrue(diff.IsDiff);
			Assert.AreEqual(1, diff.ColumnsAdded.Count);

			//test delete
			diff = t2.Compare(t1);
			Assert.IsTrue(diff.IsDiff);
			Assert.AreEqual(1, diff.ColumnsDropped.Count);

			//test diff
			t1.Columns.Items[0].Length = 20;
			diff = t1.Compare(t2);
			Assert.IsTrue(diff.IsDiff);
			Assert.AreEqual(1, diff.ColumnsDiff.Count);

			Console.WriteLine("--- create ----");
			Console.Write(t1.ScriptCreate());

			Console.WriteLine("--- migrate up ---");
			Console.Write(t1.Compare(t2).Script());

			Console.WriteLine("--- migrate down ---");
			Console.Write(t2.Compare(t1).Script());
		}

		[Test]
		public void TestExportData() {
			var t = new Table("dbo", "Status");
			t.Columns.Add(new Column("id", "int", false, null));
			t.Columns.Add(new Column("code", "char", 1, false, null));
			t.Columns.Add(new Column("description", "varchar", 20, false, null));
			t.Columns.Find("id").Identity = new Identity(1, 1);
			t.Constraints.Add(new Constraint("PK_Status", "PRIMARY KEY", "id"));

			var conn = TestHelper.GetConnString("TESTDB");
			DBHelper.DropDb(conn);
			DBHelper.CreateDb(conn);
			SqlConnection.ClearAllPools();
			DBHelper.ExecBatchSql(conn, t.ScriptCreate());

			var dataIn =
				@"1	R	Ready
2	P	Processing
3	F	Frozen
";
			var filename = Path.GetTempFileName();

			var writer = File.AppendText(filename);
			writer.Write(dataIn);
			writer.Flush();
			writer.Close();

			t.ImportData(conn, filename);
			var sw = new StringWriter();
			t.ExportData(conn, sw);
			Assert.AreEqual(dataIn, sw.ToString());

			File.Delete(filename);
		}

		[Test]
		public void TestScript() {
			//create a table with all known types, script it, and execute the script
			var t = new Table("dbo", "AllTypesTest");
			t.Columns.Add(new Column("a", "bigint", false, null));
			t.Columns.Add(new Column("b", "binary", 50, false, null));
			t.Columns.Add(new Column("c", "bit", false, null));
			t.Columns.Add(new Column("d", "char", 10, false, null));
			t.Columns.Add(new Column("e", "datetime", false, null));
			t.Columns.Add(new Column("f", "decimal", 18, 0, false, null));
			t.Columns.Add(new Column("g", "float", false, null));
			t.Columns.Add(new Column("h", "image", false, null));
			t.Columns.Add(new Column("i", "int", false, null));
			t.Columns.Add(new Column("j", "money", false, null));
			t.Columns.Add(new Column("k", "nchar", 10, false, null));
			t.Columns.Add(new Column("l", "ntext", false, null));
			t.Columns.Add(new Column("m", "numeric", 18, 0, false, null));
			t.Columns.Add(new Column("n", "nvarchar", 50, false, null));
			t.Columns.Add(new Column("o", "nvarchar", -1, false, null));
			t.Columns.Add(new Column("p", "real", false, null));
			t.Columns.Add(new Column("q", "smalldatetime", false, null));
			t.Columns.Add(new Column("r", "smallint", false, null));
			t.Columns.Add(new Column("s", "smallmoney", false, null));
			t.Columns.Add(new Column("t", "sql_variant", false, null));
			t.Columns.Add(new Column("u", "text", false, null));
			t.Columns.Add(new Column("v", "timestamp", false, null));
			t.Columns.Add(new Column("w", "tinyint", false, null));
			t.Columns.Add(new Column("x", "uniqueidentifier", false, null));
			t.Columns.Add(new Column("y", "varbinary", 50, false, null));
			t.Columns.Add(new Column("z", "varbinary", -1, false, null));
			t.Columns.Add(new Column("aa", "varchar", 50, true, new Default("DF_AllTypesTest_aa", "'asdf'")));
			t.Columns.Add(new Column("bb", "varchar", -1, true, null));
			t.Columns.Add(new Column("cc", "xml", true, null));

			Console.WriteLine(t.ScriptCreate());
			TestHelper.ExecSql(t.ScriptCreate(), "");
			TestHelper.ExecSql("drop table [dbo].[AllTypesTest]", "");
		}

		[Test]
		[ExpectedException(typeof (NotSupportedException))]
		public void TestScriptNonSupportedColumn() {
			var t = new Table("dbo", "bla");
			t.Columns.Add(new Column("a", "madeuptype", true, null));
			t.ScriptCreate();
		}
	}
}
