using NUnit.Framework;
using SchemaZen.Library.Models;

namespace SchemaZen.Tests {
	[TestFixture]
	class UserTester {
		[Test]
		public void TestUserNameShouldBeEscaped() {
			var user = new SqlUser("foo.bar", "dbo");
			var createScript = user.ScriptCreate();

			StringAssert.StartsWith("CREATE USER [foo.bar]", createScript);
		}
	}
}
