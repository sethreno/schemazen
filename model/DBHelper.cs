using System;
using System.Data.SqlClient;
using System.IO;
using SchemaZen.Library.Models;

namespace SchemaZen.Library {
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
			var initialCatalog = cnBuilder.InitialCatalog;

		    var dbName = "[" + initialCatalog + "]";

		    if (DbExists(cnBuilder.ToString())) {
				cnBuilder.InitialCatalog = "master";
				ExecSql(cnBuilder.ToString(), "ALTER DATABASE " + dbName + " SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
				ExecSql(cnBuilder.ToString(), "drop database " + dbName);

				cnBuilder.InitialCatalog = initialCatalog;
				ClearPool(cnBuilder.ToString());
			}
		}

        public static void CreateDb(string connection, string databaseFilesPath = null) {
			var cnBuilder = new SqlConnectionStringBuilder(connection);
			var dbName = cnBuilder.InitialCatalog;
			cnBuilder.InitialCatalog = "master";
		    var files = string.Empty;
		    if (databaseFilesPath != null) {
		        Directory.CreateDirectory(databaseFilesPath);
		        files = string.Format(@"ON 
(NAME = {0},
    FILENAME = '{1}\{2}.mdf')
LOG ON
(NAME = {0}_log,
    FILENAME =  '{1}\{2}.ldf')", dbName, databaseFilesPath, dbName + Guid.NewGuid() );
		    }
		    ExecSql(cnBuilder.ToString(), "CREATE DATABASE [" + dbName + "]" + files);
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
