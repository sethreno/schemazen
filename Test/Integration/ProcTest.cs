using Microsoft.Extensions.Logging;
using SchemaZen.Library.Models;
using Test.Integration.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Test.Integration;

[Trait("Category", "Integration")]
public class ProcTester {
	private readonly TestDbHelper _dbHelper;

	private readonly ILogger _logger;

	public ProcTester(ITestOutputHelper output, TestDbHelper dbHelper) {
		_logger = output.BuildLogger();
		_dbHelper = dbHelper;
	}

	[Fact]
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
		await testDb.ExecBatchSqlAsync(getAddress.ScriptCreate());
	}
}
