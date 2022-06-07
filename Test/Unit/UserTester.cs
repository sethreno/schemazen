using SchemaZen.Library.Models;
using Xunit;

namespace Test.Unit;

public class UserTest {
	[Fact]
	public void TestUserNameShouldBeEscaped() {
		var user = new SqlUser("foo.bar", "dbo");
		var createScript = user.ScriptCreate();

		Assert.StartsWith("CREATE USER [foo.bar]", createScript);
	}
}
