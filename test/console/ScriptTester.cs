using NUnit.Framework;
using SchemaZen.console;

namespace SchemaZen.test.console {
	[TestFixture]
	internal class ScriptTester {
		[Test]
		public void TestParse() {
			var cmd = new Script();
			string[] args = {
				"script",
				"cn:server=localhost;database=DEVDB;Trusted_Connection=yes;",
				"d:\\DEVDB",
				"-d",
				"--data",
				"^Lookup"
			};

			//Assert.IsTrue(cmd.Parse(args));
		}
	}
}
