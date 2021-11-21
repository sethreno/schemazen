using Xunit;
using SchemaZen.Library.Models;

namespace SchemaZen.Tests {

	public class UserTester {

		[Fact]
		public void TestUserNameShouldBeEscaped() {
			var user = new SqlUser("foo.bar", "dbo");
			var createScript = user.ScriptCreate();

			Assert.StartsWith("CREATE USER [foo.bar]", createScript);
		}
	}
}
