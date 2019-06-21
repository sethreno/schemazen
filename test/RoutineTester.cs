using NUnit.Framework;
using SchemaZen.Library.Models;

namespace SchemaZen.Tests {
	[TestFixture]
	internal class RoutineTester {

		[TestCase("ALTER", "{0}")]
		[TestCase("ALtEr", "{0}")]
		[TestCase("ALTER", "{0}")]
		[TestCase("alter", "{0}")]
		[TestCase("Alter", "{0}")]
		[TestCase("alTer", "{0}")]
		[TestCase("alteR", "{0}")]
		[TestCase(@" ALTER", "{0}")]
		[TestCase(@"	ALTER", "{0}")]
		[TestCase(@"
ALTER", "{0}")]
		[TestCase(@"   ALTER", "{0}")]
		[TestCase(@"			ALTER", "{0}")]
		[TestCase("ALTER", @"/*asfasdfa
ALTER 
sdfasdfasdf*/
{0} -- inline ALTER comment
/* here's another 
ALTER comment */

")]
[TestCase("ALTER", @"-- asfasdfa ALTER sdfasdfasdf
{0} -- inline ALTER comment
-- here's another ALTER comment

")]
		public void TestScriptShouldChangeAlterToCreateAndHandleComments(string alterPrefix, string prefixFormat) {
			var routine = new Routine(string.Empty, "test_view", null);
			var definition_main = " test_view as select stuff from things";
			routine.Text = string.Format(prefixFormat, alterPrefix) + definition_main;
			var createdScript = routine.ScriptCreate();
			var newPrefix = string.Format(prefixFormat, "CREATE");
			Assert.That(createdScript.Contains(newPrefix + definition_main), "Created script did not correctly replace the 'ALTER' statement with 'CREATE'.\r\nActual Result: \"" + createdScript + "\"");
		}

	}
}
