using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;

namespace SchemaZen.model {
	public class Schema : INameable, IHasOwner, IScriptable {
		public string Name { get; set; }
		public string Owner { get; set; }

		public Schema(string name, string owner) {
			Owner = owner;
			Name = name;
		}

		public string ScriptCreate() {
			return String.Format(@"
if not exists(select s.schema_id from sys.schemas s where s.name = '{0}') 
	and exists(select p.principal_id from sys.database_principals p where p.name = '{1}') begin
	exec sp_executesql N'create schema [{0}] authorization [{1}]'
end
", Name, Owner);
		}
	}

	public class Table : INameable, IHasOwner, IScriptable {
		private const string fieldSeparator = "\t";
		private const string escapeFieldSeparator = "--SchemaZenFieldSeparator--";
		private const string rowSeparator = "\r\n";
		private const string escapeRowSeparator = "--SchemaZenRowSeparator--";
		private const string nullValue = "--SchemaZenNull--";
		private const int rowsInBatch = 15000;

		public ColumnList Columns = new ColumnList();
		public List<Constraint> Constraints = new List<Constraint>();
		public string Name { get; set; }
		public string Owner { get; set; }
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
			foreach (var c in Columns.Items) {
				var c2 = t.Columns.Find(c.Name);
				if (c2 == null) {
					diff.ColumnsAdded.Add(c);
				} else {
					//compare mutual columns
					var cDiff = c.Compare(c2);
					if (cDiff.IsDiff) {
						diff.ColumnsDiff.Add(cDiff);
					}
				}
			}

			//get deletions
			foreach (var c in t.Columns.Items.Where(c => Columns.Find(c.Name) == null)) {
				diff.ColumnsDropped.Add(c);
			}

			//get added and compare mutual constraints
			foreach (var c in Constraints) {
				var c2 = t.FindConstraint(c.Name);
				if (c2 == null) {
					diff.ConstraintsAdded.Add(c);
				} else {
					if (c.ScriptCreate() != c2.ScriptCreate()) {
						diff.ConstraintsChanged.Add(c);
					}
				}
			}
			//get deleted constraints
			foreach (var c in t.Constraints.Where(c => FindConstraint(c.Name) == null)) {
				diff.ConstraintsDeleted.Add(c);
			}

			return diff;
		}

		public string ScriptCreate() {
			var text = new StringBuilder();
			text.AppendFormat("CREATE {2} [{0}].[{1}] {3}(\r\n", Owner, Name, IsType ? "TYPE" : "TABLE",
				IsType ? "AS TABLE " : string.Empty);
			text.Append(Columns.Script());
			if (Constraints.Count > 0) text.AppendLine();
			foreach (var c in Constraints.Where(c => c.Type != "INDEX")) {
				text.AppendLine("   ," + c.ScriptCreate());
			}
			text.AppendLine(")");
			text.AppendLine();
			foreach (var c in Constraints.Where(c => c.Type == "INDEX")) {
				text.AppendLine(c.ScriptCreate());
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
			foreach (var c in Columns.Items.Where(c => string.IsNullOrEmpty(c.ComputedDefinition))) {
				sql.AppendFormat("[{0}],", c.Name);
			}
			sql.Remove(sql.Length - 1, 1);
			sql.AppendFormat(" from [{0}].[{1}]", Owner, Name);
			if (!string.IsNullOrEmpty(tableHint))
				sql.AppendFormat(" WITH ({0})", tableHint);
			using (var cn = new SqlConnection(conn)) {
				cn.Open();
				using (var cm = cn.CreateCommand()) {
					cm.CommandText = sql.ToString();
					using (var dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							foreach (var c in Columns.Items) {
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

		public void ImportData(string conn, string filename) {
			if (IsType)
				throw new InvalidOperationException();

			var dt = new DataTable();
			foreach (var c in Columns.Items.Where(c => string.IsNullOrEmpty(c.ComputedDefinition))) {
				dt.Columns.Add(new DataColumn(c.Name, c.SqlTypeToNativeType()));
			}

			var linenumber = 0;
			var batch_rows = 0;
			SqlBulkCopy bulk;

			using (var file = new StreamReader(filename)) {
				var line = new List<char>();
				while (file.Peek() >= 0) {
					var rowsep_cnt = 0;
					line.Clear();

					while (file.Peek() >= 0) {
						var ch = (char) file.Read();
						line.Add(ch);

						if (ch == rowSeparator[rowsep_cnt])
							rowsep_cnt++;
						else
							rowsep_cnt = 0;

						if (rowsep_cnt == rowSeparator.Length) {
							// Remove rowseparator from line
							line.RemoveRange(line.Count - rowSeparator.Length, rowSeparator.Length);
							break;
						}
					}
					linenumber++;

					// Skip empty lines
					if (line.Count == 0)
						continue;

					batch_rows ++;

					var row = dt.NewRow();
					var fields = (new String(line.ToArray())).Split(new[] {fieldSeparator}, StringSplitOptions.None);
					if (fields.Length != dt.Columns.Count) {
						throw new DataException("Incorrect number of columns", linenumber);
					}
					for (var j = 0; j < fields.Length; j++) {
						try {
							row[j] = ConvertType(Columns.Items[j].Type,
								fields[j].Replace(escapeRowSeparator, rowSeparator).Replace(escapeFieldSeparator, fieldSeparator));
						} catch (FormatException ex) {
							throw new DataException(string.Format("{0} at column {1}", ex.Message, j + 1), linenumber);
						}
					}
					dt.Rows.Add(row);

					if (batch_rows == rowsInBatch) {
						batch_rows = 0;
						bulk = new SqlBulkCopy(conn,
							SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.TableLock);
						bulk.DestinationTableName = string.Format("[{0}].[{1}]", Owner, Name);
						bulk.WriteToServer(dt);
						bulk.Close();
						dt.Clear();
					}
				}
			}

			bulk = new SqlBulkCopy(conn,
				SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.TableLock);
			bulk.DestinationTableName = string.Format("[{0}].[{1}]", Owner, Name);
			bulk.WriteToServer(dt);
			bulk.Close();
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
				case "image":
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

			foreach (var c in ColumnsAdded) {
				text.AppendFormat("ALTER TABLE [{0}].[{1}] ADD {2}\r\n", Owner, Name, c.ScriptCreate());
			}

			foreach (var c in ColumnsDropped) {
				text.AppendFormat("ALTER TABLE [{0}].[{1}] DROP COLUMN [{2}]\r\n", Owner, Name, c.Name);
			}

			foreach (var c in ColumnsDiff) {
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
