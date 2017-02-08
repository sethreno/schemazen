using NUnit.Framework;
using SchemaZen.Library.Models;

namespace SchemaZen.Tests {

	[TestFixture]
	public class ConstraintTester {

		[TestFixture]
		public class ScriptCreate {

			private static Constraint SetUp() {
				return new Constraint("test", "INDEX", "a,b") {
					Table = new Table("dbo", "test")
				};
			}

			[Test]
			public void clustered_index() {
				var c = SetUp();
				c.IndexType = "CLUSTERED";
				Assert.AreEqual(
					"CREATE CLUSTERED INDEX [test] ON [dbo].[test] ([a], [b])",
					c.ScriptCreate());
			}

			[Test]
			public void nonclustered_index() {
				var c = SetUp();
				c.IndexType = "NONCLUSTERED";
				Assert.AreEqual(
					"CREATE NONCLUSTERED INDEX [test] ON [dbo].[test] ([a], [b])",
					c.ScriptCreate());
			}

			[Test]
			public void clustered_columnstore_index() {
				var c = SetUp();
				c.IndexType = "CLUSTERED COLUMNSTORE";
				Assert.AreEqual(
					"CREATE CLUSTERED COLUMNSTORE INDEX [test] ON [dbo].[test] ([a], [b])",
					c.ScriptCreate());
			}

			[Test]
			public void nonclustered_columnstore_index() {
				var c = SetUp();
				c.IndexType = "NONCLUSTERED COLUMNSTORE";
				Assert.AreEqual(
					"CREATE NONCLUSTERED COLUMNSTORE INDEX [test] ON [dbo].[test] ([a], [b])",
					c.ScriptCreate());
			}

			[Test]
			public void primary_key() {
				var c = SetUp();
				c.Type = "PRIMARY KEY";
				c.IndexType = "NONCLUSTERED";
				Assert.AreEqual(
					"CONSTRAINT [test] PRIMARY KEY NONCLUSTERED ([a], [b])",
					c.ScriptCreate());
			}

			[Test]
			public void foreign_key() {
				var c = SetUp();
				c.Type = "FOREIGN KEY";
				c.IndexType = "NONCLUSTERED";
				Assert.AreEqual(
					"CONSTRAINT [test] FOREIGN KEY NONCLUSTERED ([a], [b])",
					c.ScriptCreate());
			}

			[Test]
			public void check_constraint() {
				var c = Constraint.CreateCheckedConstraint("test", true, "[a]>(1)");
				Assert.AreEqual(
					"CONSTRAINT [test] CHECK NOT FOR REPLICATION [a]>(1)",
					c.ScriptCreate());
			}
		}
	}
}
