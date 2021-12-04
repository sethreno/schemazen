using SchemaZen.Library.Models;
using Xunit;

namespace SchemaZen.Tests;

public class ColumnTester {
	public class ScriptCreate {
		[Fact]
		public void int_no_trailing_space() {
			var c = new Column("test", "int", false, null);
			Assert.Equal("[test] [int] NOT NULL", c.ScriptCreate());
		}

		[Fact]
		public void varchar_no_trailing_space() {
			var c = new Column("test", "varchar", 10, false, null);
			Assert.Equal("[test] [varchar](10) NOT NULL", c.ScriptCreate());
		}

		[Fact]
		public void decimal_no_trailing_space() {
			var c = new Column("test", "decimal", 4, 2, false, null);
			Assert.Equal("[test] [decimal](4,2) NOT NULL", c.ScriptCreate());
		}

		[Fact]
		public void no_trailing_space_with_default() {
			var c = new Column("test", "int", true, new Default("df_test", "0", false));
			var lines = c.ScriptCreate()
				.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			Assert.Equal("[test] [int] NULL", lines[0]);
			Assert.Equal("      CONSTRAINT [df_test] DEFAULT 0", lines[1]);
		}

		[Fact]
		public void no_trailing_space_with_no_name_default() {
			var c = new Column("test", "int", true, new Default("df_ABCDEF", "0", true));
			var lines = c.ScriptCreate()
				.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			Assert.Equal("[test] [int] NULL", lines[0]);
			Assert.Equal("       DEFAULT 0", lines[1]);
		}

		[Fact]
		public void computed_column() {
			var c = new Column("test", "int", false, null) { ComputedDefinition = "(A + B)" };

			Assert.Equal("[test] AS (A + B)", c.ScriptCreate());
		}

		[Fact]
		public void computed_column_persisted() {
			var c = new Column("test", "int", false, null)
				{ ComputedDefinition = "(A + B)", Persisted = true };

			Assert.Equal("[test] AS (A + B) PERSISTED", c.ScriptCreate());
		}
	}
}
