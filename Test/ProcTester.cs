using Microsoft.Extensions.Logging;
using SchemaZen.Library.Models;
using Xunit;
using Xunit.Abstractions;

namespace SchemaZen.Tests;

public class ProcTester {
	private readonly TestDbHelper _dbHelper;

	private readonly ILogger _logger;

	public ProcTester(ITestOutputHelper output, TestDbHelper dbHelper) {
		_logger = output.BuildLogger();
		_dbHelper = dbHelper;
	}

	[Fact]
	[Trait("Category", "Integration")]
	public async Task TestScript() {
		var t = new Table("dbo", "Address");
		t.Columns.Add(new Column("id", "int", false, null));
		t.Columns.Add(new Column("street", "varchar", 50, false, null));
		t.Columns.Add(new Column("city", "varchar", 50, false, null));
		t.Columns.Add(new Column("state", "char", 2, false, null));
		t.Columns.Add(new Column("zip", "char", 5, false, null));
		t.AddConstraint(new Constraint("PK_Address", "PRIMARY KEY", "id"));

		var getAddress = new Routine("dbo", "GetAddress", null);
		getAddress.Text = @"
CREATE PROCEDURE [dbo].[GetAddress]
	@id int
AS
	select * from Address where id = @id
";

		await using var testDb = await _dbHelper.CreateTestDbAsync();
		await testDb.ExecSqlAsync(t.ScriptCreate());
		testDb.ExecBatchSql(getAddress.ScriptCreate());
	}

	[Fact]
	public void TestScriptWarnings() {
		const string baseText = @"--example of routine that has been renamed since creation
CREATE PROCEDURE {0}
	@id int
AS
	select * from Address where id = @id
";
		var getAddress = new Routine("dbo", "GetAddress", null);
		getAddress.RoutineType = Routine.RoutineKind.Procedure;

		getAddress.Text = string.Format(baseText, "[dbo].[NamedDifferently]");
		Assert.True(getAddress.Warnings().Any());
		getAddress.Text = string.Format(baseText, "dbo.NamedDifferently");
		Assert.True(getAddress.Warnings().Any());

		getAddress.Text = string.Format(baseText, "dbo.[GetAddress]");
		Assert.False(getAddress.Warnings().Any());

		getAddress.Text = string.Format(baseText, "dbo.GetAddress");
		Assert.False(getAddress.Warnings().Any());

		getAddress.Text = string.Format(baseText, "GetAddress");
		Assert.False(getAddress.Warnings().Any());
	}
}
