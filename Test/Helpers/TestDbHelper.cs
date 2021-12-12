using System.Data.SqlClient;
using SchemaZen.Library;

namespace SchemaZen.Tests;

public class TestDbHelper {
	private readonly string _masterDbConnString;
	private readonly string _masterDbName;

	public TestDbHelper(string masterDbConnString) {
		_masterDbConnString = masterDbConnString;
		_masterDbName = new SqlConnectionStringBuilder(_masterDbConnString).InitialCatalog;
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

	public void ExecBatchSql(string sql, string? dbName = null) {
		// todo make async version
		// low priority: currently referenced by 5 tests 
		dbName = dbName ?? _masterDbName;
		DBHelper.ExecBatchSql(GetConnString(dbName), sql);
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
