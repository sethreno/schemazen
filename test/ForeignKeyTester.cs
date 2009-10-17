using model;
using NUnit.Framework;

namespace test {
    [TestFixture()]
    public class ForeignKeyTester {

        [Test()]
        public void TestScript() {
            Table person = new Table("dbo", "Person");
            person.Columns.Add(new Column("id", "int", false, null));
            person.Columns.Add(new Column("name", "varchar", 50, false, null));
            person.Columns.Find("id").Identity = new Identity(1, 1);
            person.Constraints.Add(new model.Constraint("PK_Person", "PRIMARY KEY", "id"));

            Table address = new Table("dbo", "Address");
            address.Columns.Add(new Column("id", "int", false, null));
            address.Columns.Add(new Column("personId", "int", false, null));
            address.Columns.Add(new Column("street", "varchar", 50, false, null));
            address.Columns.Add(new Column("city", "varchar", 50, false, null));
            address.Columns.Add(new Column("state", "char", 2, false, null));
            address.Columns.Add(new Column("zip", "varchar", 5, false, null));
            address.Columns.Find("id").Identity = new Identity(1, 1);
            address.Constraints.Add(new model.Constraint("PK_Address", "PRIMARY KEY", "id"));

            ForeignKey fk = new ForeignKey(address, "FK_Address_Person", "personId", person, "id", "", "CASCADE");

            TestHelper.ExecSql(person.ScriptCreate(), "");
            TestHelper.ExecSql(address.ScriptCreate(), "");
            TestHelper.ExecSql(fk.ScriptCreate(), "");
            TestHelper.ExecSql("drop table Address", "");
            TestHelper.ExecSql("drop table Person", "");
        }

    }


}