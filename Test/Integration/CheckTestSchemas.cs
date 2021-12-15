using Microsoft.Extensions.Logging;
using SchemaZen.Library.Models;
using Test.Integration.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Test.Integration;

[Trait("Category", "Integration")]
public class CheckTestSchemas {
	private readonly TestDbHelper _dbHelper;

	private readonly ILogger _logger;

	public CheckTestSchemas(ITestOutputHelper output, TestDbHelper dbHelper) {
		_logger = output.BuildLogger();
		_dbHelper = dbHelper;
	}

	[Fact]
	public async Task test_bop_quote() {
		var script = Path.Combine("Integration/Schemas", "BOP_QUOTE.sql");
		await TestCopySchema(script, "bop");
	}

	[Fact]
	public async Task test_bop_quote_2() {
		var script = Path.Combine("Integration/Schemas", "BOP_QUOTE_2.sql");
		await TestCopySchema(script, "bop2");
	}

	[Fact]
	public async Task test_dfs_quote() {
		var script = Path.Combine("Integration/Schemas", "DFS_QUOTE.sql");
		await TestCopySchema(script, "dfs");
	}

	[Fact]
	public async Task test_fk_refs_non_pk_col() {
		var script = Path.Combine("Integration/Schemas", "FK_REFS_NON_PK_COL.sql");
		await TestCopySchema(script, "fk_refs_non_pk_col");
	}

	[Fact]
	public async Task test_ims_quote() {
		var script = Path.Combine("Integration/Schemas", "IMS_QUOTE.sql");
		await TestCopySchema(script, "ims_quote");
	}

	[Fact]
	public async Task test_sandbox3() {
		var script = Path.Combine("Integration/Schemas", "SANDBOX3_GBL.SQL");
		var copy = await TestCopySchema(script, "sandbox3");

		Assert.Equal(
			"SQL_Latin1_General_CP1_CI_AS",
			copy.FindProp("COLLATE").Value);
	}

	private async Task<Database> TestCopySchema(
		string pathToSchemaScript,
		string dbSuffix) {
		var sourceDbName = $"CopySchemaSource_{dbSuffix}";
		var destDbName = $"CopySchemaDest_{dbSuffix}";

		//create the db from sql script
		_logger.LogInformation($"creating db {sourceDbName}");
		await _dbHelper.CreateDbAsync(sourceDbName);
		await _dbHelper.ExecBatchSqlAsync(File.ReadAllText(pathToSchemaScript), sourceDbName);

		//load the model from newly created db and create a copy
		var copy = new Database(destDbName);
		copy.Connection = _dbHelper.GetConnString(sourceDbName);
		copy.Load();
		var scripted = copy.ScriptCreate();
		await _dbHelper.ExecBatchSqlAsync(scripted);

		//compare the dbs to make sure they are the same
		var source = new Database(sourceDbName) {
			Connection = _dbHelper.GetConnString(sourceDbName)
		};
		source.Load();
		copy.Load();

		await _dbHelper.DropDbAsync(sourceDbName);
		await _dbHelper.DropDbAsync(destDbName);

		Assert.False(source.Compare(copy).IsDiff);

		return copy;
	}
}
