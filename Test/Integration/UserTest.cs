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
		// todo - fix the bug causing this test to fail
		// I think Roles need to be updated in order to fix this.
		// Currently they execute as one big script with all their permissions
		//
		// or maybe users need to be updated so they don't reference roles
		// the problem here appears a circular dependency...
		// user depends on role
		// schema depends on user
		// view depends on schema
		// role depends on view
		//
		//   .>  role  -,
		//   |          v
		// user       view
		//   ^          |
		//    \ schema <
		//
		// Maybe create a new object Permissions that gets scripted in a seprate
		// stage similar to foreign_keys
		var testSchema = @"

CREATE ROLE [MyRole]
GO

IF SUSER_ID('usr') IS NULL BEGIN
CREATE LOGIN usr WITH PASSWORD = 0x0100A92164F026C6EFC652DE59D9DEF79AC654E4E8EFA8E01A9B HASHED END
CREATE USER [usr] FOR LOGIN usr WITH DEFAULT_SCHEMA = dbo
exec sp_addrolemember 'MyRole', 'usr'
exec sp_addrolemember 'db_datareader', 'usr'
GO

create schema [TestSchema] authorization [usr]
GO

CREATE VIEW TestSchema.a_view AS SELECT 1 AS N
GO

GRANT SELECT ON [TestSchema].[a_view] TO [MyRole]
GO
        ";

		await using var testDb = await _dbHelper.CreateTestDbAsync();
		await testDb.ExecBatchSqlAsync(testSchema);

		var db = new Database(_dbHelper.MakeTestDbName(), logger: _logger);
		db.Connection = testDb.GetConnString();
		db.Load();
		db.Dir = db.Name;
		db.ScriptToDir();

		var ex = Record.Exception(() => db.CreateFromDir(true));
		Assert.Null(ex);

		Assert.NotNull(db.Routines.FirstOrDefault(x => x.Name == "a_view"));
		Assert.NotNull(db.Roles.FirstOrDefault(x => x.Name == "MyRole"));
		Assert.NotNull(db.Users.FirstOrDefault(x => x.Name == "usr"));
		Assert.NotNull(db.Schemas.FirstOrDefault(x => x.Name == "TestSchema"));
	}
}
