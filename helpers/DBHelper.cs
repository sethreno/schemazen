using System;
using System.Data.SqlClient;
using SchemaZen.model;

namespace SchemaZen.helpers {
	public class DBHelper {
		public static bool EchoSql = false;

		public static void ExecSql(string conn, string sql) {
			if (EchoSql) Console.WriteLine(sql);
			using (var cn = new SqlConnection(conn)) {
				cn.Open();
				using (var cm = cn.CreateCommand()) {
					cm.CommandText = sql;
					cm.ExecuteNonQuery();
				}
			}
		}

		public static void ExecBatchSql(string conn, string sql) {
			var prevLines = 0;
			using (var cn = new SqlConnection(conn)) {
				cn.Open();
				using (var cm = cn.CreateCommand()) {
					foreach (var script in BatchSqlParser.SplitBatch(sql)) {
						if (EchoSql) Console.WriteLine(script);
						cm.CommandText = script;
						try {
							cm.ExecuteNonQuery();
						} catch (SqlException ex) {
							throw new SqlBatchException(ex, prevLines);
						}

						prevLines += script.Split('\n').Length;
						prevLines += 1; // add one line for GO statement
					}
				}
			}
		}

		public static void DropDb(string conn) {
			var cnBuilder = new SqlConnectionStringBuilder(conn);
			var dbName = cnBuilder.InitialCatalog;
			if (DbExists(cnBuilder.ToString())) {
				cnBuilder.InitialCatalog = "master";
				ExecSql(cnBuilder.ToString(), "ALTER DATABASE " + dbName + " SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
				ExecSql(cnBuilder.ToString(), "drop database " + dbName);

				cnBuilder.InitialCatalog = dbName;
				ClearPool(cnBuilder.ToString());
			}
		}

		public static void CreateDb(string conn) {
			var cnBuilder = new SqlConnectionStringBuilder(conn);
			var dbName = cnBuilder.InitialCatalog;
			cnBuilder.InitialCatalog = "master";
			ExecSql(cnBuilder.ToString(), "CREATE DATABASE " + dbName);
		}

		public static bool DbExists(string conn) {
			var exists = false;
			var cnBuilder = new SqlConnectionStringBuilder(conn);
			var dbName = cnBuilder.InitialCatalog;
			cnBuilder.InitialCatalog = "master";

			using (var cn = new SqlConnection(cnBuilder.ToString())) {
				cn.Open();
				using (var cm = cn.CreateCommand()) {
					cm.CommandText = "select db_id('" + dbName + "')";
					exists = (!ReferenceEquals(cm.ExecuteScalar(), DBNull.Value));
				}
			}

			return exists;
		}

		public static void ClearPool(string conn) {
			using (var cn = new SqlConnection(conn)) {
				SqlConnection.ClearPool(cn);
			}
		}
	}
}
