using System.Linq;
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
			t.AddConstraint(new Constraint("PK_Address", "PRIMARY KEY", "id"));

			var getAddress = new Routine("dbo", "GetAddress", null);
			getAddress.Text = @"
CREATE PROCEDURE [dbo].[GetAddress]
	@id int
AS
	select * from Address where id = @id
";

			TestHelper.ExecSql(t.ScriptCreate(), "");
			TestHelper.ExecBatchSql(getAddress.ScriptCreate() + "\nGO", "");

			TestHelper.ExecSql("drop table [dbo].[Address]", "");
			TestHelper.ExecSql("drop procedure [dbo].[GetAddress]", "");
		}

		[Test]
		public void TestScriptWarnings() {
			const string baseText = @"--example of routine that has been renamed since creation
CREATE PROCEDURE {0}
	@id int
AS
	select * from Address where id = @id
";
			var getAddress = new Routine("dbo", "GetAddress", null);
			getAddress.RoutineType = Routine.RoutineKind.Procedure;

			getAddress.Text = string.Format(baseText, "[dbo].[NamedDifferently]");
			Assert.IsTrue(getAddress.Warnings().Any());
			getAddress.Text = string.Format(baseText, "dbo.NamedDifferently");
			Assert.IsTrue(getAddress.Warnings().Any());

			getAddress.Text = string.Format(baseText, "dbo.[GetAddress]");
			Assert.IsFalse(getAddress.Warnings().Any());

			getAddress.Text = string.Format(baseText, "dbo.GetAddress");
			Assert.IsFalse(getAddress.Warnings().Any());

			getAddress.Text = string.Format(baseText, "GetAddress");
			Assert.IsFalse(getAddress.Warnings().Any());
		}
	}
}
