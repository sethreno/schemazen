using System;
using NUnit.Framework;
using SchemaZen.Library.Models;

namespace SchemaZen.Tests {

	[TestFixture]
	public class ColumnTester {

		[TestFixture]
		public class  ScriptCreate {

			[Test]
			public void int_no_trailing_space() {
				var c = new Column("test", "int", false, null);
				Assert.AreEqual("[test] [int] NOT NULL", c.ScriptCreate());
			}

			[Test]
			public void varchar_no_trailing_space() {
				var c = new Column("test", "varchar", 10, false, null);
				Assert.AreEqual("[test] [varchar](10) NOT NULL", c.ScriptCreate());
			}

			[Test]
			public void decimal_no_trailing_space() {
				var c = new Column("test", "decimal", 4, 2, false, null);
				Assert.AreEqual("[test] [decimal](4,2) NOT NULL", c.ScriptCreate());
			}

			[Test]
			public void no_trailing_space_with_default() {
				var c = new Column("test", "int", true, new Default("df_test", "0", false));
				var lines = c.ScriptCreate().Split(new []{Environment.NewLine}, StringSplitOptions.None);
				Assert.AreEqual("[test] [int] NULL", lines[0]);
				Assert.AreEqual("      CONSTRAINT [df_test] DEFAULT 0", lines[1]);
			}

            [Test]
            public void no_trailing_space_with_no_name_default()
            {
                var c = new Column("test", "int", true, new Default("df_ABCDEF", "0", true));
                var lines = c.ScriptCreate().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                Assert.AreEqual("[test] [int] NULL", lines[0]);
                Assert.AreEqual("       DEFAULT 0", lines[1]);
            }



            [Test]
            public void computed_column()
            {
                var c = new Column("test", "int", false, null) {ComputedDefinition = "(A + B)"};

                Assert.AreEqual("[test] AS (A + B)", c.ScriptCreate());
            }

            [Test]
            public void computed_column_persisted()
            {
                var c = new Column("test", "int", false, null) {ComputedDefinition = "(A + B)", Persisted = true};

                Assert.AreEqual("[test] AS (A + B) PERSISTED", c.ScriptCreate());
            }
        }
	}
}
