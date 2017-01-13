using System.Collections.Generic;
using NUnit.Framework;
using SchemaZen.Library.Models;

namespace SchemaZen.Tests {
	[TestFixture]
	public class AssemblyTester {
		[Test]
		[TestCase("SAFE_ACCESS", "SAFE")]
		[TestCase("UNSAFE_ACCESS", "UNSAFE")]
		[TestCase("EXTERNAL_ACCESS", "EXTERNAL_ACCESS")]
		public void Assembly_WithPermissionSetCases(string permissionSet, string scriptedPermissionSet) {
			var assembly = new SqlAssembly(permissionSet, "SchemazenAssembly");
			assembly.Files.Add(new KeyValuePair<string, byte[]>("mydll", new byte[0]));

			var expected = "CREATE ASSEMBLY [SchemazenAssembly]\r\nFROM 0x\r\nWITH PERMISSION_SET = " + scriptedPermissionSet;
			Assert.AreEqual(expected, assembly.ScriptCreate());
		}
	}
}