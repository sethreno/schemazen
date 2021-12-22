using System.Data.SqlClient;
using SchemaZen.Library;
using SchemaZen.Library.Models;

namespace Test.Integration.Helpers;

public class TestDbHelper {
	private readonly string _masterDbConnString;
	private readonly string _masterDbName;

	public TestDbHelper(string masterDbConnString) {
		_masterDbConnString = masterDbConnString;
		_masterDbName = new SqlConnectionStringBuilder(_masterDbConnString).InitialCatalog;
	}

	public string MakeTestDbName() {
		return $"testDb{Guid.NewGuid()}".Replace("-", "");
	}

	public async Task<TestDb> CreateTestDbAsync(string? dbName = null) {
		if (dbName == null) dbName = MakeTestDbName();
		await CreateDbAsync(dbName);
		return new TestDb(dbName, this);
	}

	public async Task<TestDb> CreateTestDb(Database db) {
		if (string.IsNullOrEmpty(db.Dir)) db.Dir = db.Name;
		if (string.IsNullOrEmpty(db.Connection))
			db.Connection = GetConnString(db.Name);

		// todo make async ScriptToDir and get rid of Task.Run
		await Task.Run(
			() => {
				db.ScriptToDir();
				db.CreateFromDir(false); // no overwrite - db should not exist
			});

		return new TestDb(db.Name, this);
	}

	public string GetConnString(string dbName) {
		var connString = new SqlConnectionStringBuilder(_masterDbConnString);
		connString.InitialCatalog = dbName;
		return connString.ToString();
	}

	public async Task<SqlConnection> CreateOpenConnectionAsync(string dbName) {
		var cn = new SqlConnection(GetConnString(dbName));
		await cn.OpenAsync();
		return cn;
	}

	public async Task ExecSqlAsync(string sql, string? dbName = null) {
		dbName = dbName ?? _masterDbName;
		using var cn = await CreateOpenConnectionAsync(dbName);
		using var cm = cn.CreateCommand();
		cm.CommandText = sql;
		await cm.ExecuteNonQueryAsync();
	}

	public async Task ExecBatchSqlAsync(string sql, string? dbName = null) {
		// todo make async version
		// low priority: currently referenced by 5 tests 
		dbName = dbName ?? _masterDbName;
		await DBHelper.ExecBatchSqlAsync(GetConnString(dbName), sql);
	}

	public async Task CreateDbAsync(string dbName) {
		if (await DbExistsAsync(dbName))
			await DropDbAsync(dbName);

		await ExecSqlAsync($"create database {dbName}", _masterDbName);
	}

	public async Task DropDbAsync(string dbName) {
		if (!await DbExistsAsync(dbName)) return;

		await ExecSqlAsync(
			$"ALTER DATABASE {dbName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE",
			_masterDbName);

		await ExecSqlAsync($"drop database {dbName}", _masterDbName);
	}

	public async Task<bool> DbExistsAsync(string dbName) {
		using var cn = await CreateOpenConnectionAsync(_masterDbName);
		using var cm = cn.CreateCommand();
		cm.CommandText = "select db_id('" + dbName + "')";
		return !ReferenceEquals(cm.ExecuteScalar(), DBNull.Value);
	}
}

public sealed class TestDb : IAsyncDisposable {
	private readonly TestDbHelper _helper;

	public TestDb(string name, TestDbHelper helper) {
		DbName = name;
		_helper = helper;
	}

	public string DbName { get; }

	public async ValueTask DisposeAsync() {
		await _helper.DropDbAsync(DbName);
	}

	public async Task ExecSqlAsync(string sql) {
		await _helper.ExecSqlAsync(sql, DbName);
	}

	public async Task ExecBatchSqlAsync(string sql) {
		await _helper.ExecBatchSqlAsync(sql, DbName);
	}

	public string GetConnString() {
		return _helper.GetConnString(DbName);
	}
}
