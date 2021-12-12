using Microsoft.Extensions.Logging;
using SchemaZen.Library.Models;
using Xunit;
using Xunit.Abstractions;

namespace SchemaZen.Tests;

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
		var script = Path.Combine("test_schemas", "BOP_QUOTE.sql");
		await TestCopySchema(script, "bop");
	}

	[Fact]
	public async Task test_bop_quote_2() {
		var script = Path.Combine("test_schemas", "BOP_QUOTE_2.sql");
		await TestCopySchema(script, "bop2");
	}

	[Fact]
	public async Task test_dfs_quote() {
		var script = Path.Combine("test_schemas", "DFS_QUOTE.sql");
		await TestCopySchema(script, "dfs");
	}

	[Fact]
	public async Task test_fk_refs_non_pk_col() {
		var script = Path.Combine("test_schemas", "FK_REFS_NON_PK_COL.sql");
		await TestCopySchema(script, "fk_refs_non_pk_col");
	}

	[Fact]
	public async Task test_ims_quote() {
		var script = Path.Combine("test_schemas", "IMS_QUOTE.sql");
		await TestCopySchema(script, "ims_quote");
	}

	[Fact]
	public async Task test_sandbox3() {
		var script = Path.Combine("test_schemas", "SANDBOX3_GBL.SQL");
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

		await _dbHelper.DropDbAsync(sourceDbName);
		await _dbHelper.DropDbAsync(destDbName);

		//create the db from sql script
		_logger.LogInformation($"creating db {sourceDbName}");
		await _dbHelper.ExecSqlAsync($"CREATE DATABASE {sourceDbName}");
		_dbHelper.ExecBatchSql(File.ReadAllText(pathToSchemaScript), sourceDbName);

		//load the model from newly created db and create a copy
		var copy = new Database(destDbName);
		copy.Connection = _dbHelper.GetConnString(sourceDbName);
		copy.Load();
		var scripted = copy.ScriptCreate();
		_dbHelper.ExecBatchSql(scripted);

		//compare the dbs to make sure they are the same
		var source = new Database(sourceDbName) {
			Connection = _dbHelper.GetConnString(sourceDbName)
		};
		source.Load();
		copy.Load();
		Assert.False(source.Compare(copy).IsDiff);

		return copy;
	}
}
