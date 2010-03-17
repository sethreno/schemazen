using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace model {
	public class DBHelper {
		public static bool EchoSql = false;

		public static void ExecSql(string conn, string sql) {
			if (EchoSql) Console.WriteLine(sql);
			using (SqlConnection cn = new SqlConnection(conn)) {
				cn.Open();
				using (SqlCommand cm = cn.CreateCommand()) {
					cm.CommandText = sql;
					cm.ExecuteNonQuery();
				}
			}
		}

		public static void ExecBatchSql(string conn, string sql) {
			var prevLines = 0;
			using (SqlConnection cn = new SqlConnection(conn)) {
				cn.Open();
				using (SqlCommand cm = cn.CreateCommand()) {
					foreach (string script in SplitBatchSql(sql)) {						
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

		public static string[] SplitBatchSql(string batchSql) {
            return BatchSqlParser.SplitBatch(batchSql);
		}

		public static void DropDb(string conn) {
			SqlConnectionStringBuilder cnBuilder = new SqlConnectionStringBuilder(conn);
			string dbName = cnBuilder.InitialCatalog;			
			if (DbExists(cnBuilder.ToString())) {
                cnBuilder.InitialCatalog = "master";
				ExecSql(cnBuilder.ToString(), "ALTER DATABASE " + dbName + " SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
				ExecSql(cnBuilder.ToString(), "drop database " + dbName);

				cnBuilder.InitialCatalog = dbName;
				ClearPool(cnBuilder.ToString());
			}
		}

		public static void CreateDb(string conn) {
			SqlConnectionStringBuilder cnBuilder = new SqlConnectionStringBuilder(conn);
			string dbName = cnBuilder.InitialCatalog;
			cnBuilder.InitialCatalog = "master";
			ExecSql(cnBuilder.ToString(), "CREATE DATABASE " + dbName);
		}

		public static bool DbExists(string conn) {
			bool exists = false;
			SqlConnectionStringBuilder cnBuilder = new SqlConnectionStringBuilder(conn);
			string dbName = cnBuilder.InitialCatalog;
			cnBuilder.InitialCatalog = "master";

			using (SqlConnection cn = new SqlConnection(cnBuilder.ToString())) {
				cn.Open();
				using (SqlCommand cm = cn.CreateCommand()) {
					cm.CommandText = "select db_id('" + dbName + "')";
					exists = (!object.ReferenceEquals(cm.ExecuteScalar(), DBNull.Value));
				}
			}

			return exists;
		}

		public static void ClearPool(string conn) {
			using (SqlConnection cn = new SqlConnection(conn)) {
				SqlConnection.ClearPool(cn);
			}
		}

	}

}
