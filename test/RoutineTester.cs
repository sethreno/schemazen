using System;
using NUnit.Framework;
using SchemaZen.Library.Models;

namespace SchemaZen.Tests
{
	[TestFixture]
	internal class RoutineTester
	{

		[TestCase("ALTER")]
		[TestCase("ALtEr")]
		[TestCase("ALTER")]
		[TestCase("alter")]
		[TestCase("Alter")]
		[TestCase("alTer")]
		[TestCase("alteR")]
		public void TestScriptCreateShouldReplaceAlter(string alterPrefix)
		{
			var routine = new Routine("dbo", "test_view", null){RoutineType = Routine.RoutineKind.View};
			var definition_main = "[test_view] as select stuff from things";
			routine.Text = alterPrefix + " view " + definition_main;
			var createdScript = routine.ScriptCreate();
			var newPrefix = "CREATE";
			Assert.That(createdScript.Contains(newPrefix + " view [dbo]." + definition_main), "Created script did not correctly replace the 'ALTER' statement with 'CREATE'.\r\nActual Result: \"" + createdScript + "\"");
		}

		[TestCase("", "dbo", "test_view", "expected_viewname")]
		[TestCase("dbo", "dbo", "test_view", "expected_viewname")]
		[TestCase("[dbo]", "dbo", "test_view", "expected_viewname")]
		[TestCase("owner", "dbo", "test_view", "expected_viewname")]
		[TestCase("dbo", "owner", "test_view", "expected_viewname")]
		[TestCase("[dbo]", "dbo", "test_view", "expected_viewname")]
		[TestCase("dbo", "dbo", "[test_view]", "expected_viewname")]
		[TestCase("[dbo]", "dbo", "[test_view]", "expected_viewname")]
		[TestCase("", "dbo", "[test_view]", "expected_viewname")]
		public void TestScriptShouldReplaceSchemaAndName(string owner, string expectedOwner, string name, string expectedName)
		{
			var viewContent = " as Select stuff from things";
			var routine = new Routine(expectedOwner, expectedName, null) { RoutineType = Routine.RoutineKind.View };
			if (owner.Trim() != String.Empty) { owner = owner + "."; }

			routine.Text = "CREATE view " + owner + name + viewContent;
			var createdScript = routine.ScriptCreate();
			var expectedScript = "CREATE view [" + expectedOwner + "].[" + expectedName + "]" + viewContent;

			Assert.That(createdScript.Contains(expectedScript), "Created script did not correctly replace the schema and name.\r\nActual Result: \"" + createdScript + "\"");
		}

		[TestCase(@" {0} view [dbo].[test_view] as select stuff from things")]
		[TestCase(@"	{0} view [dbo].[test_view] as select stuff from things")]
		[TestCase(@"
{0} view [dbo].[test_view] as select stuff from things")]
		[TestCase(@"   {0} view [dbo].[test_view] as select stuff from things")]
		[TestCase(@"			{0} view [dbo].[test_view] as select stuff from things")]
		[TestCase(@"{0} view /*asfasdfa
ALTER 
sdfasdfasdf*/
[dbo].[test_view] as select stuff from things -- inline ALTER comment
/* here's another 
ALTER comment */

")]
		[TestCase(@"{0} view -- asfasdfa ALTER sdfasdfasdf  
[dbo].[test_view] as select stuff from things -- inline ALTER comment
-- here's another ALTER comment

")]
		public void TestScriptShouldHandleCommentsAndSpaces(string text)
		{
			var routine = new Routine("dbo", "test_view", null) { RoutineType = Routine.RoutineKind.View };
			routine.Text = string.Format(text, "ALTER");
			var createdScript = routine.ScriptCreate();
			var expectedScript = string.Format(text, "CREATE").Trim('\r', '\n');
			Assert.That(createdScript.Contains(expectedScript), "Created script did not correctly handle comments. \r\nActual Result: \"" + createdScript + "");
		}

		[TestCase("dbo", "create view test_view as select stuff from things")]
		[TestCase("dbo", "create view [test_view] as select stuff from things")]
		[TestCase("dbo", "create view dbo.test_view as select stuff from things")]
		[TestCase("dbo", "create view dbo.[test_view] as select stuff from things")]
		[TestCase("dbo", "create view [dbo].test_view as select stuff from things")]
		[TestCase("dbo", "create view [dbo].[test_view] as select stuff from things")]
		[TestCase("owner", "create view owner.test_view as select stuff from things")]
		[TestCase("owner", "create view [owner].[test_view] as select stuff from things")]
		public void TestScriptShouldSkipRegexIfActionNameAndOwnerMatch(string owner, string text) {
			var routine = new Routine(owner, "test_view", null) {RoutineType = Routine.RoutineKind.View};
			var expectedScript = text;
			routine.Text = expectedScript;
			var createdScript = routine.ScriptCreate();
			Assert.That(createdScript.Contains(expectedScript), "Created script modified the routine text unnecessarily");
		}
	}
}
