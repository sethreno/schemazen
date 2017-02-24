using System.Linq;
using NUnit.Framework;
using SchemaZen.Library.Models;

namespace SchemaZen.Tests {
	[TestFixture]
	public class ForeignKeyTester {
		public void TestMultiColumnKey() {
			var t1 = new Table("dbo", "t1");
			t1.Columns.Add(new Column("c2", "varchar", 10, false, null));
			t1.Columns.Add(new Column("c1", "int", false, null));
			t1.AddConstraint(new Constraint("pk_t1", "PRIMARY KEY", "c1,c2"));

			var t2 = new Table("dbo", "t2");
			t2.Columns.Add(new Column("c1", "int", false, null));
			t2.Columns.Add(new Column("c2", "varchar", 10, false, null));
			t2.Columns.Add(new Column("c3", "int", false, null));

			var fk = new ForeignKey(t2, "fk_test", "c3,c2", t1, "c1,c2");

			var db = new Database("TESTDB");
			db.Tables.Add(t1);
			db.Tables.Add(t2);
			db.ForeignKeys.Add(fk);
			db.Connection = TestHelper.GetConnString("TESTDB");
			db.ExecCreate(true);
			db.Load();

			Assert.AreEqual("c3", db.FindForeignKey("fk_test", "dbo").Columns[0]);
			Assert.AreEqual("c2", db.FindForeignKey("fk_test", "dbo").Columns[1]);
			Assert.AreEqual("c1", db.FindForeignKey("fk_test", "dbo").RefColumns[0]);
			Assert.AreEqual("c2", db.FindForeignKey("fk_test", "dbo").RefColumns[1]);

			db.ExecCreate(true);
		}

		[Test]
		public void TestScript() {
			var person = new Table("dbo", "Person");
			person.Columns.Add(new Column("id", "int", false, null));
			person.Columns.Add(new Column("name", "varchar", 50, false, null));
			person.Columns.Find("id").Identity = new Identity(1, 1);
			person.AddConstraint(new Constraint("PK_Person", "PRIMARY KEY", "id"));

			var address = new Table("dbo", "Address");
			address.Columns.Add(new Column("id", "int", false, null));
			address.Columns.Add(new Column("personId", "int", false, null));
            address.Columns.Add(new Column("street", "varchar", 50, false, null));
			address.Columns.Add(new Column("city", "varchar", 50, false, null));
			address.Columns.Add(new Column("state", "char", 2, false, null));
			address.Columns.Add(new Column("zip", "varchar", 5, false, null));
			address.Columns.Find("id").Identity = new Identity(1, 1);
			address.AddConstraint(new Constraint("PK_Address", "PRIMARY KEY", "id"));

			var fk = new ForeignKey(address, "FK_Address_Person", "personId", person, "id", "", "CASCADE");

			TestHelper.ExecSql(person.ScriptCreate(), "");
			TestHelper.ExecSql(address.ScriptCreate(), "");
			TestHelper.ExecSql(fk.ScriptCreate(), "");
			TestHelper.ExecSql("drop table Address", "");
			TestHelper.ExecSql("drop table Person", "");
		}

        [Test]
        public void TestScriptForeignKeyWithNoName()
        {
            var t1 = new Table("dbo", "t1");
            t1.Columns.Add(new Column("c2", "varchar", 10, false, null));
            t1.Columns.Add(new Column("c1", "int", false, null));
            t1.AddConstraint(new Constraint("pk_t1", "PRIMARY KEY", "c1,c2"));

            var t2 = new Table("dbo", "t2");
            t2.Columns.Add(new Column("c1", "int", false, null));
            t2.Columns.Add(new Column("c2", "varchar", 10, false, null));
            t2.Columns.Add(new Column("c3", "int", false, null));

            var fk = new ForeignKey(t2, "fk_ABCDEF", "c3,c2", t1, "c1,c2");
            fk.IsSystemNamed = true;

            var db = new Database("TESTDB");
            db.Tables.Add(t1);
            db.Tables.Add(t2);
            db.ForeignKeys.Add(fk);
            db.Connection = TestHelper.GetConnString("TESTDB");
            db.ExecCreate(true);
            db.Load();

            Assert.AreEqual(1, db.ForeignKeys.Count);

            var fkCopy = db.ForeignKeys.Single();
            Assert.AreEqual("c3", fkCopy.Columns[0]);
            Assert.AreEqual("c2", fkCopy.Columns[1]);
            Assert.AreEqual("c1", fkCopy.RefColumns[0]);
            Assert.AreEqual("c2", fkCopy.RefColumns[1]);
            Assert.IsTrue(fkCopy.IsSystemNamed);

            db.ExecCreate(true);
        }
    }
}
