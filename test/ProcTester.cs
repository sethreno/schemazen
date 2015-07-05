using NUnit.Framework;
using SchemaZen.model;

namespace SchemaZen.test {
	[TestFixture]
	public class ProcTester {
		[Test]
		public void TestScript() {
			var t = new Table("dbo", "Address");
			t.Columns.Add(new Column("id", "int", false, null));
			t.Columns.Add(new Column("street", "varchar", 50, false, null));
			t.Columns.Add(new Column("city", "varchar", 50, false, null));
			t.Columns.Add(new Column("state", "char", 2, false, null));
			t.Columns.Add(new Column("zip", "char", 5, false, null));
			t.Constraints.Add(new Constraint("PK_Address", "PRIMARY KEY", "id"));

			var getAddress = new Routine("dbo", "GetAddress");
			getAddress.Text = @"
CREATE PROCEDURE [dbo].[GetAddress]
	@id int
AS
	select * from Address where id = @id
";

			TestHelper.ExecSql(t.ScriptCreate(), "");
			TestHelper.ExecBatchSql(getAddress.ScriptCreate(null) + "\nGO", "");
			TestHelper.ExecSql("drop table [dbo].[Address]", "");
			TestHelper.ExecSql("drop procedure [dbo].[GetAddress]", "");
		}

		[Test]
		public void TestScriptWrongName()
		{
			var t = new Table("dbo", "Address");
			t.Columns.Add(new Column("id", "int", false, null));
			t.Columns.Add(new Column("street", "varchar", 50, false, null));
			t.Columns.Add(new Column("city", "varchar", 50, false, null));
			t.Columns.Add(new Column("state", "char", 2, false, null));
			t.Columns.Add(new Column("zip", "char", 5, false, null));
			t.Constraints.Add(new Constraint("PK_Address", "PRIMARY KEY", "id"));

			var baseText = @"--example of routine that has been renamed since creation
CREATE PROCEDURE {0}
	@id int
AS
	select * from Address where id = @id
";
			var getAddress = new Routine("dbo", "GetAddress");
			getAddress.Text = string.Format(baseText, "[dbo].[NamedDifferently]");

			TestHelper.ExecSql(t.ScriptCreate(), "");
			var script = getAddress.ScriptCreate(null);
			Assert.IsTrue(script.Contains(string.Format(baseText, "[dbo].[GetAddress]")));
			TestHelper.ExecBatchSql(script + "\nGO", "");

			TestHelper.ExecSql("drop procedure [dbo].[GetAddress]", "");
			TestHelper.ExecSql("drop table [dbo].[Address]", "");


		}
	}
}
