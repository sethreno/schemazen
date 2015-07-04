using System;
using System.Data.SqlClient;
using NUnit.Framework;
using SchemaZen.model;

namespace SchemaZen.test {
	[SetUpFixture]
	public class TestHelper {
		public static bool EchoSql {
			get { return true; }
		}

		[SetUp]
		public void SetUp() {
			string conn = GetConnString("TESTDB");
			DBHelper.DropDb(conn);
			DBHelper.CreateDb(conn);
			SqlConnection.ClearAllPools();
			// TODO: verify that database called "model" is empty, otherwise tests will fail
		}

		public static void ExecSql(string sql, string dbName) {
			if (EchoSql) Console.WriteLine(sql);
			using (var cn = new SqlConnection(ConfigHelper.TestDB)) {
				if (!string.IsNullOrEmpty(dbName)) {
					cn.ConnectionString = GetConnString(dbName);
				}
				cn.Open();
				using (SqlCommand cm = cn.CreateCommand()) {
					cm.CommandText = sql;
					cm.ExecuteNonQuery();
				}
			}
		}

		public static void ExecBatchSql(string sql, string dbName) {
			DBHelper.ExecBatchSql(GetConnString(dbName), sql);
		}

		public static string GetConnString(string dbName) {
			string connString = "";
			using (var cn = new SqlConnection(ConfigHelper.TestDB)) {
				connString = cn.ConnectionString;
				if (!string.IsNullOrEmpty(dbName)) {
					connString = cn.ConnectionString.Replace("database=" + cn.Database, "database=" + dbName);
				}
			}
			return connString;
		}

		public static void DropDb(string dbName) {
			if (DbExists(dbName)) {
				ExecSql("ALTER DATABASE " + dbName + " SET SINGLE_USER WITH ROLLBACK IMMEDIATE", "");
				ExecSql("drop database " + dbName, "");
				ClearPool(dbName);
			}
		}

		public static bool DbExists(string dbName) {
			bool exists = false;
			using (var cn = new SqlConnection(ConfigHelper.TestDB)) {
				cn.Open();
				using (SqlCommand cm = cn.CreateCommand()) {
					cm.CommandText = "select db_id('" + dbName + "')";
					exists = (!ReferenceEquals(cm.ExecuteScalar(), DBNull.Value));
				}
			}

			return exists;
		}

		public static void ClearPool(string dbName) {
			using (var cn = new SqlConnection(GetConnString(dbName))) {
				SqlConnection.ClearPool(cn);
			}
		}
	}
}
