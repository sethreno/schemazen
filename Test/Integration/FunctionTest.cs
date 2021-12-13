using Microsoft.Extensions.Logging;
using SchemaZen.Library.Models;
using Test.Integration.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Test.Integration;

[Trait("Category", "Integration")]
public class FunctionTest {
	private const string _exampleFunc = @"
CREATE FUNCTION [dbo].udf_GetDate()
RETURNS DATETIME AS
BEGIN
	RETURN GETDATE()
END
";

	private readonly TestDbHelper _dbHelper;

	private readonly ILogger _logger;

	public FunctionTest(ITestOutputHelper output, TestDbHelper dbHelper) {
		_logger = output.BuildLogger();
		_dbHelper = dbHelper;
	}

	[Fact]
	public async Task TestScript() {
		var f = new Routine("dbo", "udf_GetDate", null) {
			RoutineType = Routine.RoutineKind.Function,
			Text = _exampleFunc
		};

		await using var testDb = await _dbHelper.CreateTestDbAsync();

		// script includes GO so use ExecBatch
		testDb.ExecBatchSql(f.ScriptCreate());
	}
}
