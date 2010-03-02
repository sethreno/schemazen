using model;
using NUnit.Framework;

namespace test {
    [TestFixture()]
    public class ProcTester {

        [Test()]
        public void TestScript() {
            Table t = new Table("dbo", "Address");
            t.Columns.Add(new Column("id", "int", false, null));
            t.Columns.Add(new Column("street", "varchar", 50, false, null));
            t.Columns.Add(new Column("city", "varchar", 50, false, null));
            t.Columns.Add(new Column("state", "char", 2, false, null));
            t.Columns.Add(new Column("zip", "char", 5, false, null));
            t.Constraints.Add(new model.Constraint("PK_Address", "PRIMARY KEY", "id"));

            Routine getAddress = new Routine("dbo", "GetAddress");
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

    }

}