using SchemaZen.Library.Models;
using Xunit;

namespace SchemaZen.Tests;

public class AssemblyTester {
	[Theory]
	[InlineData("SAFE_ACCESS", "SAFE")]
	[InlineData("UNSAFE_ACCESS", "UNSAFE")]
	[InlineData("EXTERNAL_ACCESS", "EXTERNAL_ACCESS")]
	public void Assembly_WithPermissionSetCases(
		string permissionSet,
		string scriptedPermissionSet
	) {
		var assembly = new SqlAssembly(permissionSet, "SchemazenAssembly");
		assembly.Files.Add(new KeyValuePair<string, byte[]>("mydll", new byte[0]));

		var expected = @"CREATE ASSEMBLY [SchemazenAssembly]
FROM 0x
WITH PERMISSION_SET = " + scriptedPermissionSet;

		Assert.Equal(expected, assembly.ScriptCreate());
	}
}
