using Microsoft.Extensions.Logging;
using SchemaZen.Library.Models;
using Xunit;
using Xunit.Abstractions;

namespace SchemaZen.Tests;

public class FunctionTester {
	private const string _exampleFunc = @"
CREATE FUNCTION [dbo].udf_GetDate()
RETURNS DATETIME AS
BEGIN
	RETURN GETDATE()
END
";

	private readonly TestDbHelper _dbHelper;

	private readonly ILogger _logger;

	public FunctionTester(ITestOutputHelper output, TestDbHelper dbHelper) {
		_logger = output.BuildLogger();
		_dbHelper = dbHelper;
	}

	[Fact]
	[Trait("Category", "Integration")]
	public async Task TestScript() {
		var f = new Routine("dbo", "udf_GetDate", null) {
			RoutineType = Routine.RoutineKind.Function,
			Text = _exampleFunc
		};

		await using var testDb = await _dbHelper.CreateTestDbAsync();

		// script includes GO so use ExecBatch
		testDb.ExecBatchSql(f.ScriptCreate());
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
