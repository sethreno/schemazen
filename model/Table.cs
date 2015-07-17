using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;

namespace SchemaZen.model {
	public class Schema {
		public string Name;
		public string Owner;

		public Schema(string name, string owner) {
			Owner = owner;
			Name = name;
		}
	}

	public class Table {
		private const string fieldSeparator = "\t";
		private const string escapeFieldSeparator = "--SchemaZenFieldSeparator--";
		private const string rowSeparator = "\r\n";
		private const string escapeRowSeparator = "--SchemaZenRowSeparator--";
		private const string nullValue = "--SchemaZenNull--";
		public ColumnList Columns = new ColumnList();
		public List<Constraint> Constraints = new List<Constraint>();
		public string Name;
		public string Owner;
		public bool IsType;

		public Table(string owner, string name) {
			Owner = owner;
			Name = name;
		}

		public Constraint PrimaryKey {
			get { return Constraints.FirstOrDefault(c => c.Type == "PRIMARY KEY"); }
		}

		public Constraint FindConstraint(string name) {
			return Constraints.FirstOrDefault(c => c.Name == name);
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
				} else {
					//compare mutual columns
					ColumnDiff cDiff = c.Compare(c2);
					if (cDiff.IsDiff) {
						diff.ColumnsDiff.Add(cDiff);
					}
				}
			}

			//get deletions
			foreach (Column c in t.Columns.Items.Where(c => Columns.Find(c.Name) == null)) {
				diff.ColumnsDropped.Add(c);
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
			foreach (Constraint c in t.Constraints.Where(c => FindConstraint(c.Name) == null)) {
				diff.ConstraintsDeleted.Add(c);
			}

			return diff;
		}

		public string ScriptCreate() {
			var text = new StringBuilder();
			text.AppendFormat("CREATE {2} [{0}].[{1}] {3}(\r\n", Owner, Name, IsType ? "TYPE" : "TABLE", IsType ? "AS TABLE " : string.Empty);
			text.Append(Columns.Script());
			if (Constraints.Count > 0) text.AppendLine();
			foreach (Constraint c in Constraints.Where(c => c.Type != "INDEX")) {
				text.AppendLine("   ," + c.Script());
			}
			text.AppendLine(")");
			text.AppendLine();
			foreach (Constraint c in Constraints.Where(c => c.Type == "INDEX")) {
				text.AppendLine(c.Script());
			}
			return text.ToString();
		}

		public string ScriptDrop() {
			return string.Format("DROP {2} [{0}].[{1}]", Owner, Name, IsType ? "TYPE" : "TABLE");
		}


		public void ExportData(string conn, TextWriter data, string tableHint = null) {
			if (IsType)
				throw new InvalidOperationException();

			var sql = new StringBuilder();
			sql.Append("select ");
			foreach (Column c in Columns.Items.Where(c => string.IsNullOrEmpty(c.ComputedDefinition))) {
				sql.AppendFormat("[{0}],", c.Name);
			}
			sql.Remove(sql.Length - 1, 1);
			sql.AppendFormat(" from [{0}].[{1}]", Owner, Name);
			if (!string.IsNullOrEmpty(tableHint))
				sql.AppendFormat(" WITH ({0})", tableHint);
			using (var cn = new SqlConnection(conn)) {
				cn.Open();
				using (SqlCommand cm = cn.CreateCommand()) {
					cm.CommandText = sql.ToString();
					using (SqlDataReader dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							foreach (Column c in Columns.Items) {
								if (dr[c.Name] is DBNull)
									data.Write(nullValue);
								else if (dr[c.Name] is byte[])
									data.Write(new SoapHexBinary((byte[]) dr[c.Name]).ToString());
								else
									data.Write(dr[c.Name].ToString()
										.Replace(fieldSeparator, escapeFieldSeparator)
										.Replace(rowSeparator, escapeRowSeparator));
								if (c != Columns.Items.Last())
									data.Write(fieldSeparator);
							}
							data.WriteLine();
						}
					}
				}
			}
		}

		public void ImportData(string conn, string data) {
			if (IsType)
				throw new InvalidOperationException();

			var dt = new DataTable();
			foreach (Column c in Columns.Items.Where(c => string.IsNullOrEmpty(c.ComputedDefinition))) {
				dt.Columns.Add(new DataColumn(c.Name, c.SqlTypeToNativeType()));
			}
			string[] lines = data.Split(new[] {rowSeparator}, StringSplitOptions.RemoveEmptyEntries);
			int i = 0;
			foreach (string line in lines) {
				i++;
				DataRow row = dt.NewRow();
				string[] fields = line.Split(new[] {fieldSeparator}, StringSplitOptions.None);
				if (fields.Length != dt.Columns.Count) {
					throw new DataException("Incorrect number of columns", i);
				}
				for (int j = 0; j < fields.Length; j++) {
					try {
						row[j] = ConvertType(Columns.Items[j].Type,
							fields[j].Replace(escapeRowSeparator, rowSeparator).Replace(escapeFieldSeparator, fieldSeparator));
					} catch (FormatException ex) {
						throw new DataException(string.Format("{0} at column {1}", ex.Message, j + 1), i);
					}
				}
				dt.Rows.Add(row);
			}

			var bulk = new SqlBulkCopy(conn,
				SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.TableLock);
			bulk.DestinationTableName = Name;
			bulk.WriteToServer(dt);
		}

		public static object ConvertType(string sqlType, string val) {
			if (val == nullValue)
				return DBNull.Value;

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
					return int.Parse(val);
				case "uniqueidentifier":
					return new Guid(val);
				case "varbinary":
					return SoapHexBinary.Parse(val).Value;
				default:
					return val;
			}
		}
	}

	public class TableDiff {
		public List<Column> ColumnsAdded = new List<Column>();
		public List<ColumnDiff> ColumnsDiff = new List<ColumnDiff>();
		public List<Column> ColumnsDropped = new List<Column>();

		public List<Constraint> ConstraintsAdded = new List<Constraint>();
		public List<Constraint> ConstraintsChanged = new List<Constraint>();
		public List<Constraint> ConstraintsDeleted = new List<Constraint>();
		public string Name;
		public string Owner;

		public bool IsDiff {
			get {
				return ColumnsAdded.Count + ColumnsDropped.Count + ColumnsDiff.Count + ConstraintsAdded.Count +
				       ConstraintsChanged.Count + ConstraintsDeleted.Count > 0;
			}
		}

		public string Script() {
			var text = new StringBuilder();

			foreach (Column c in ColumnsAdded) {
				text.AppendFormat("ALTER TABLE [{0}].[{1}] ADD {2}\r\n", Owner, Name, c.ScriptCreate());
			}

			foreach (Column c in ColumnsDropped) {
				text.AppendFormat("ALTER TABLE [{0}].[{1}] DROP COLUMN [{2}]\r\n", Owner, Name, c.Name);
			}

			foreach (ColumnDiff c in ColumnsDiff) {
				if (c.DefaultIsDiff) {
					if (c.Source.Default != null) {
						text.AppendFormat("ALTER TABLE [{0}].[{1}] {2}\r\n", Owner, Name, c.Source.Default.ScriptDrop());
					}
					if (c.Target.Default != null) {
						text.AppendFormat("ALTER TABLE [{0}].[{1}] {2}\r\n", Owner, Name, c.Target.Default.ScriptCreate(c.Target));
					}
				}
				if (!c.OnlyDefaultIsDiff) {
					text.AppendFormat("ALTER TABLE [{0}].[{1}] ALTER COLUMN {2}\r\n", Owner, Name, c.Target.ScriptAlter());
				}
			}
			return text.ToString();
		}
	}
}
