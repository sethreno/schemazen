using Microsoft.Extensions.Logging;
using SchemaZen.Library.Models;
using Test.Integration.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Test.Integration;

[Trait("Category", "Integration")]
public class UserTest {
	private readonly TestDbHelper _dbHelper;

	private readonly ILogger _logger;

	public UserTest(ITestOutputHelper output, TestDbHelper dbHelper) {
		_logger = output.BuildLogger();
		_dbHelper = dbHelper;
	}

	[Fact]
	public async Task TestScriptUserAssignedToRole() {
		var testSchema = @"
CREATE VIEW a_view AS SELECT 1 AS N
GO

CREATE ROLE [MyRole]
GO

GRANT SELECT ON [dbo].[a_view] TO [MyRole]
GO

IF SUSER_ID('usr') IS NULL BEGIN
CREATE LOGIN usr WITH PASSWORD = 0x0100A92164F026C6EFC652DE59D9DEF79AC654E4E8EFA8E01A9B HASHED END
CREATE USER [usr] FOR LOGIN usr WITH DEFAULT_SCHEMA = dbo
exec sp_addrolemember 'MyRole', 'usr'
exec sp_addrolemember 'db_datareader', 'usr'
GO

        ";

		await using var testDb = await _dbHelper.CreateTestDbAsync();

		await testDb.ExecBatchSqlAsync(testSchema);

		var db = new Database(_dbHelper.MakeTestDbName());
		db.Connection = testDb.GetConnString();
		db.Load();
		db.Dir = db.Name;
		db.ScriptToDir();

		db.Load();

		var ex = Record.Exception(() => db.CreateFromDir(true));
		Assert.Null(ex);
	}

	[Fact]
	public async Task TestScriptUserAssignedToSchema() {
		var testSchema = @"
CREATE VIEW a_view AS SELECT 1 AS N
GO

CREATE ROLE [MyRole]
GO

GRANT SELECT ON [dbo].[a_view] TO [MyRole]
GO

IF SUSER_ID('usr') IS NULL BEGIN
CREATE LOGIN usr WITH PASSWORD = 0x0100A92164F026C6EFC652DE59D9DEF79AC654E4E8EFA8E01A9B HASHED END
CREATE USER [usr] FOR LOGIN usr WITH DEFAULT_SCHEMA = dbo
exec sp_addrolemember 'MyRole', 'usr'
exec sp_addrolemember 'db_datareader', 'usr'
GO

        ";

		await using var testDb = await _dbHelper.CreateTestDbAsync();

		await testDb.ExecBatchSqlAsync(testSchema);

		var db = new Database(_dbHelper.MakeTestDbName());
		db.Connection = testDb.GetConnString();
		db.Load();
		db.Dir = db.Name;
		db.ScriptToDir();

		db.Load();

		var ex = Record.Exception(() => db.CreateFromDir(true));
		Assert.Null(ex);
	}
}
