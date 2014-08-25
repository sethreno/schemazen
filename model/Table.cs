using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace model {
	public class Schema
	{
		public string Name;
		public string Owner;

	    private Schema()
	    {
	    }

		public Schema(string name, string owner)
		{
			Owner = owner;
			Name = name;
		}
	}

	public class Table {
		public ColumnList Columns = new ColumnList();
		public List<Constraint> Constraints = new List<Constraint>();
		public string Name;
		public string Owner;

	    private Table() { }

		public Table(string owner, string name) {
			Owner = owner;
			Name = name;
		}

		public Constraint PrimaryKey {
			get {
				foreach (Constraint c in Constraints) {
					if (c.Type == "PRIMARY KEY") return c;
				}
				return null;
			}
		}

		public Constraint FindConstraint(string name) {
			foreach (Constraint c in Constraints) {
				if (c.Name == name) return c;
			}
			return null;
		}

		public TableDiff Compare(Table t) {
			var diff = new TableDiff();
			diff.Owner = t.Owner;
			diff.Name = t.Name;

			//get additions and compare mutual columns
			foreach (Column c in Columns.Items) {
				Column c2 = t.Columns.Find(c.Name);
				if (c2 == null) {
					diff.ColumnsAdded.Add(c);
				}
				else {
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
				}
				else {
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
			var text = new StringBuilder();
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
			var data = new StringBuilder();
			var sql = new StringBuilder();
			sql.Append("select ");
			foreach (Column c in Columns.Items) {
				sql.AppendFormat("[{0}],", c.Name);
			}
			sql.Remove(sql.Length - 1, 1);
			sql.AppendFormat(" from [{0}].[{1}]", Owner, Name);
			using (var cn = new SqlConnection(conn)) {
				cn.Open();
				using (SqlCommand cm = cn.CreateCommand()) {
					cm.CommandText = sql.ToString();
					using (SqlDataReader dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							foreach (Column c in Columns.Items) {
								data.AppendFormat("{0}\t", dr[c.Name]);
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
			var dt = new DataTable();
			foreach (Column c in Columns.Items) {
				dt.Columns.Add(new DataColumn(c.Name));
			}
			string[] lines = data.Split("\r\n".Split(','), StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < lines.Count(); i++) {
				string line = lines[i];
				DataRow row = dt.NewRow();
				string[] fields = line.Split('\t');
				if (fields.Length != Columns.Items.Count) {
					throw new DataException("Incorrect number of columns", i + 1);
				}
				for (int j = 0; j < fields.Length; j++) {
					try {
						row[j] = ConvertType(Columns.Items[j].Type, fields[j]);
					}
					catch (FormatException ex) {
						throw new DataException(String.Format("{0} at column {1}", ex.Message, j + 1), i + 1);
					}
				}
				dt.Rows.Add(row);
			}

			var bulk = new SqlBulkCopy(conn,
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
					return DateTime.Parse(val);
				case "int":
					int.Parse(val);
					return val;
				default:
					return val;
			}
		}
	}

	public class TableDiff {
		public List<Column> ColumnsAdded = new List<Column>();
		public List<ColumnDiff> ColumnsDiff = new List<ColumnDiff>();
		public List<Column> ColumnsDroped = new List<Column>();

		public List<Constraint> ConstraintsAdded = new List<Constraint>();
		public List<Constraint> ConstraintsChanged = new List<Constraint>();
		public List<Constraint> ConstraintsDeleted = new List<Constraint>();
		public string Name;
		public string Owner;

		public bool IsDiff {
			get {
				return ColumnsAdded.Count + ColumnsDroped.Count + ColumnsDiff.Count + ConstraintsAdded.Count +
				       ConstraintsChanged.Count + ConstraintsDeleted.Count > 0;
			}
		}

		public string Script() {
			var text = new StringBuilder();

			foreach (Column c in ColumnsAdded) {
				text.AppendFormat("ALTER TABLE [{0}].[{1}] ADD {2}\r\n", Owner, Name, c.Script());
			}

			foreach (Column c in ColumnsDroped) {
				text.AppendFormat("ALTER TABLE [{0}].[{1}] DROP COLUMN [{2}]\r\n", Owner, Name, c.Name);
			}

			foreach (ColumnDiff c in ColumnsDiff) {
				text.AppendFormat("ALTER TABLE [{0}].[{1}] ALTER COLUMN {2}\r\n", Owner, Name, c.Script());
			}
			return text.ToString();
		}
	}
}