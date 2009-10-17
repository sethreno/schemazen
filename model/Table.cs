using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace model {

	public class Table {
		public Table(string owner, string name) {
			this.Owner = owner;
			this.Name = name;
		}
		public string Owner;
		public string Name;
		public ColumnList Columns = new ColumnList();
		public List<Constraint> Constraints = new List<Constraint>();

		public Constraint FindConstraint(string name) {
			foreach (Constraint c in Constraints) {
				if (c.Name == name) return c;
			}
			return null;
		}

		public Constraint PrimaryKey {
			get {
				foreach (Constraint c in Constraints) {
					if (c.Type == "PRIMARY KEY") return c;
				}
				return null;
			}
		}

		public TableDiff Compare(Table t) {
			TableDiff diff = new TableDiff();
			diff.Owner = t.Owner;
			diff.Name = t.Name;

			//get additions and compare mutual columns
			foreach (Column c in Columns.Items) {
				Column c2 = t.Columns.Find(c.Name);
				if (c2 == null) {
					diff.ColumnsAdded.Add(c);
				} else {
					//compare mutual columns
					ColumnDiff cDiff = c.Compare(c2);
					if (cDiff.IsDiff) {
						diff.ColumnsDiff.Add(cDiff);
					}
				}
			}

			//get deletions
			foreach (Column c in t.Columns.Items) {
				if (Columns.Find(c.Name) == null) {
					diff.ColumnsDroped.Add(c);
				}
			}

			//get added and compare mutual constraints
			foreach (Constraint c in Constraints) {
				Constraint c2 = t.FindConstraint(c.Name);
				if (c2 == null) {
					diff.ConstraintsAdded.Add(c);
				} else {
					if (c.Script() != c2.Script()) {
						diff.ConstraintsChanged.Add(c);
					}
				}
			}
			//get deleted constraints
			foreach (Constraint c in t.Constraints) {
				if (FindConstraint(c.Name) == null) {
					diff.ConstraintsDeleted.Add(c);
				}
			}

			return diff;
		}

		public string ScriptCreate() {
			StringBuilder text = new StringBuilder();
			text.AppendFormat("CREATE TABLE [{0}].[{1}](\r\n", Owner, Name);
			text.Append(Columns.Script());
			if (Constraints.Count > 0) text.AppendLine();
			foreach (Constraint c in Constraints) {
				if (c.Type == "INDEX") continue;
				text.AppendLine("   ," + c.Script());
			}
			text.AppendLine(")");
			text.AppendLine();
			foreach (Constraint c in Constraints) {
				if (c.Type != "INDEX") continue;
				text.AppendLine(c.Script());
			}
			return text.ToString();
		}

		public string ScriptDrop() {
			return string.Format("DROP TABLE [{0}].[{1}]", Owner, Name);
		}

		public string ExportData(string conn) {
			StringBuilder data = new StringBuilder();
			StringBuilder sql = new StringBuilder();
			sql.Append("select ");
			foreach (Column c in Columns.Items) {
				sql.AppendFormat("{0},", c.Name);
			}
			sql.Remove(sql.Length - 1, 1);
			sql.AppendFormat(" from {0}", Name);
			using (SqlConnection cn = new SqlConnection(conn)) {
				cn.Open();
				using (SqlCommand cm = cn.CreateCommand()) {
					cm.CommandText = sql.ToString();
					using (SqlDataReader dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							foreach (Column c in Columns.Items) {
								data.AppendFormat("{0}\t", dr[c.Name].ToString());
							}
							data.Remove(data.Length - 1, 1);
							data.AppendLine();
						}
					}
				}
			}

			return data.ToString();
		}

		public void ImportData(string conn, string data) {
			DataTable dt = new DataTable();
			foreach (Column c in Columns.Items) {
				dt.Columns.Add(new DataColumn(c.Name));
			}
			foreach (string line in data.Split("\r\n".Split(','), StringSplitOptions.RemoveEmptyEntries)) {
				DataRow row = dt.NewRow();
				string[] fields = line.Split('\t');
				for (int i = 0; i <= fields.Length - 1; i++) {
					row[i] = ConvertType(Columns.Items[i].Type, fields[i]);
				}
				dt.Rows.Add(row);
			}
			SqlBulkCopy bulk = new SqlBulkCopy(conn, 
				SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.TableLock);
			bulk.DestinationTableName = Name;
			bulk.WriteToServer(dt);
		}

		public object ConvertType(string sqlType, string val) {
			if (val.Length == 0) return DBNull.Value;
			switch (sqlType.ToLower()) {
				case "bit":
					//added for compatibility with bcp
					if (val == "0") val = "False";
					if (val == "1") val = "True";
					return bool.Parse(val);
				case "datetime":
				case "smalldatetime":
					return System.DateTime.Parse(val);
				default:
					return val;
			}
		}
	}

	public class TableDiff {
		public string Owner;
		public string Name;

		public List<Column> ColumnsAdded = new List<Column>();
		public List<Column> ColumnsDroped = new List<Column>();
		public List<ColumnDiff> ColumnsDiff = new List<ColumnDiff>();

		public List<Constraint> ConstraintsAdded = new List<Constraint>();
		public List<Constraint> ConstraintsChanged = new List<Constraint>();
		public List<Constraint> ConstraintsDeleted = new List<Constraint>();

		public bool IsDiff {
			get { return ColumnsAdded.Count + ColumnsDroped.Count + ColumnsDiff.Count + ConstraintsAdded.Count + ConstraintsChanged.Count + ConstraintsDeleted.Count > 0; }
		}

		public string Script() {
			StringBuilder text = new StringBuilder();

			foreach (Column c in ColumnsAdded) {
				text.AppendFormat("ALTER TABLE [{0}].[{1}] ADD {2}", Owner, Name, c.Script());
				text.AppendLine();
			}

			foreach (Column c in ColumnsDroped) {
				text.AppendFormat("ALTER TABLE [{0}].[{1}] DROP COLUMN [{2}]", Owner, Name, c.Name);
				text.AppendLine();
			}

			foreach (ColumnDiff c in ColumnsDiff) {
				text.AppendFormat("ALTER TABLE [{0}].[{1}] ALTER COLUMN {2}", Owner, Name, c.Script());
				text.AppendLine();
			}
			text.AppendLine("GO");
			text.AppendLine();

			return text.ToString();
		}
	}
}
