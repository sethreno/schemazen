using System;
using model;
using NUnit.Framework;

namespace test {
	[TestFixture]
	public class FunctionTester {
		[Test]
		public void TestScript() {
			var f = new Routine("dbo", "udf_GetDate");
			f.Text = @"
CREATE FUNCTION [dbo].[udf_GetDate]()
RETURNS DATETIME AS
BEGIN
	RETURN GETDATE()
END
";
			Console.WriteLine(f.ScriptCreate(null));
			TestHelper.ExecBatchSql(f.ScriptCreate(null) + "\nGO", "");
			TestHelper.ExecSql("drop function [dbo].[udf_GetDate]", "");
		}
	}
}
