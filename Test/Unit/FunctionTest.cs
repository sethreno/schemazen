using Microsoft.Extensions.Logging;
using SchemaZen.Library.Models;
using Xunit;
using Xunit.Abstractions;

namespace Test.Unit;

public class FunctionTest {
	private const string _exampleFunc = @"
CREATE FUNCTION [dbo].udf_GetDate()
RETURNS DATETIME AS
BEGIN
	RETURN GETDATE()
END
";

	private readonly ILogger _logger;

	public FunctionTest(ITestOutputHelper output) {
		_logger = output.BuildLogger();
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
