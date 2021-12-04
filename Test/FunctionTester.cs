using SchemaZen.Library.Models;
using Xunit;

namespace SchemaZen.Tests; 

[Collection("TestDb")]
public class FunctionTester {
	private const string _exampleFunc = @"
CREATE FUNCTION [dbo].udf_GetDate()
RETURNS DATETIME AS
BEGIN
	RETURN GETDATE()
END
";

	[Fact]
	public void TestScript() {
		var f = new Routine("dbo", "udf_GetDate", null) {
			RoutineType = Routine.RoutineKind.Function,
			Text = _exampleFunc
		};
		Console.WriteLine(f.ScriptCreate());
		TestHelper.ExecBatchSql(f.ScriptCreate() + "\nGO", "");
		TestHelper.ExecSql("drop function [dbo].[udf_GetDate]", "");
	}

	[Fact]
	public void TestScriptNoWarnings() {
		var f = new Routine("dbo", "udf_GetDate", null) {
			Text = _exampleFunc,
			RoutineType = Routine.RoutineKind.Function
		};
		Assert.False(f.Warnings().Any());
	}

	[Fact]
	public void TestScriptWarnings() {
		var f = new Routine("dbo", "udf_GetDate2", null) {
			Text = _exampleFunc,
			RoutineType = Routine.RoutineKind.Function
		};
		Assert.True(f.Warnings().Any());
	}
}
