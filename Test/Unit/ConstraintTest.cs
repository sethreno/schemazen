using SchemaZen.Library.Models;
using Xunit;

namespace Test.Unit;

public class ConstraintTest {
	public class ScriptCreate {
		private static Constraint SetUp() {
			return new Constraint("test", "INDEX", "a,b") {
				Table = new Table("dbo", "test")
			};
		}

		[Fact]
		public void clustered_index() {
			var c = SetUp();
			c.IndexType = "CLUSTERED";
			Assert.Equal(
				"CREATE CLUSTERED INDEX [test] ON [dbo].[test] ([a], [b])",
				c.ScriptCreate());
		}

		[Fact]
		public void nonclustered_index() {
			var c = SetUp();
			c.IndexType = "NONCLUSTERED";
			Assert.Equal(
				"CREATE NONCLUSTERED INDEX [test] ON [dbo].[test] ([a], [b])",
				c.ScriptCreate());
		}

		[Fact]
		public void clustered_columnstore_index() {
			var c = SetUp();
			c.IndexType = "CLUSTERED COLUMNSTORE";
			Assert.Equal(
				"CREATE CLUSTERED COLUMNSTORE INDEX [test] ON [dbo].[test] ([a], [b])",
				c.ScriptCreate());
		}

		[Fact]
		public void nonclustered_columnstore_index() {
			var c = SetUp();
			c.IndexType = "NONCLUSTERED COLUMNSTORE";
			Assert.Equal(
				"CREATE NONCLUSTERED COLUMNSTORE INDEX [test] ON [dbo].[test] ([a], [b])",
				c.ScriptCreate());
		}

		[Fact]
		public void primary_key() {
			var c = SetUp();
			c.Type = "PRIMARY KEY";
			c.IndexType = "NONCLUSTERED";
			Assert.Equal(
				"CONSTRAINT [test] PRIMARY KEY NONCLUSTERED ([a], [b])",
				c.ScriptCreate());
		}

		[Fact]
		public void foreign_key() {
			var c = SetUp();
			c.Type = "FOREIGN KEY";
			c.IndexType = "NONCLUSTERED";
			Assert.Equal(
				"CONSTRAINT [test] FOREIGN KEY NONCLUSTERED ([a], [b])",
				c.ScriptCreate());
		}

		[Fact]
		public void check_constraint() {
			var c = Constraint.CreateCheckedConstraint("test", true, false, "[a]>(1)");
			Assert.Equal(
				"CHECK NOT FOR REPLICATION [a]>(1)",
				c.ScriptCreate());
		}
	}
}
