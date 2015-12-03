using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;

namespace SchemaZen.model {
	public class Schema : INameable, IHasOwner { 
		public string Name { get; set; }
		public string Owner { get; set; }

		public Schema(string name, string owner) {
			Owner = owner;
			Name = name;
		}

		public string ScriptCreate() {
			return string.Format(@"
				if not exists(select s.schema_id from sys.schemas s where s.name = '{0}') 
					and exists(select p.principal_id from sys.database_principals p where p.name = '{1}') begin
					exec sp_executesql N'create schema [{0}] authorization [{1}]'
				end
				", Name, Owner);
		}
	}

	public class Table : INameable, IHasOwner {
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
			return new TableDiff(this, t);
		}

		public string ScriptCreate() {
			var text = new StringBuilder();
			text.AppendFormat("CREATE {2} [{0}].[{1}] {3}(", Owner, Name, IsType ? "TYPE" : "TABLE",
				IsType ? "AS TABLE " : string.Empty);
			text.AppendLine();
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
			var cols = Columns.Items.Where(c => string.IsNullOrEmpty(c.ComputedDefinition)).ToArray();
			foreach (var c in cols) {
				sql.AppendFormat("[{0}],", c.Name);
			}
			sql.Remove(sql.Length - 1, 1);
			sql.AppendFormat(" from [{0}].[{1}]", Owner, Name);
			if (!string.IsNullOrEmpty(tableHint))
				sql.AppendFormat(" with ({0})", tableHint);
			using (var cn = new SqlConnection(conn)) {
				cn.Open();
				using (var cm = cn.CreateCommand()) {
					cm.CommandText = sql.ToString();
					using (var dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							foreach (var c in cols) {
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
			var cols = Columns.Items.Where(c => string.IsNullOrEmpty(c.ComputedDefinition)).ToArray();
            foreach (var c in cols) {
				dt.Columns.Add(new DataColumn(c.Name, c.SqlTypeToNativeType()));
			}

			var linenumber = 0;
			var batchRows = 0;
			using (var bulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.TableLock)) {
				foreach (var colName in dt.Columns.OfType<DataColumn>().Select(c => c.ColumnName))
					bulk.ColumnMappings.Add(colName, colName);
				bulk.DestinationTableName = string.Format("[{0}].[{1}]", Owner, Name);

				using (var file = new StreamReader(filename)) {
					var line = new List<char>();
					while (file.Peek() >= 0) {
						var rowsepCnt = 0;
						line.Clear();

						while (file.Peek() >= 0) {
							var ch = (char)file.Read();
							line.Add(ch);

							if (ch == rowSeparator[rowsepCnt])
								rowsepCnt++;
							else
								rowsepCnt = 0;

							if (rowsepCnt == rowSeparator.Length) {
								// Remove rowseparator from line
								line.RemoveRange(line.Count - rowSeparator.Length, rowSeparator.Length);
								break;
							}
						}
						linenumber++;

						// Skip empty lines
						if (line.Count == 0)
							continue;

						batchRows ++;

						var row = dt.NewRow();
						var fields = (new String(line.ToArray())).Split(new[] {
																				  fieldSeparator
																			  }, StringSplitOptions.None);
						if (fields.Length != dt.Columns.Count) {
							throw new DataFileException("Incorrect number of columns", filename, linenumber);
						}
						for (var j = 0; j < fields.Length; j++) {
							try {
								row[j] = ConvertType(cols[j].Type, fields[j].Replace(escapeRowSeparator, rowSeparator).Replace(escapeFieldSeparator, fieldSeparator));
							} catch (FormatException ex) {
								throw new DataFileException(string.Format("{0} Column number: {1} Column name: {2} Column type: {3} Value: {4}", ex.Message, j + 1, cols[j].Name, cols[j].Type, fields[j]), filename, linenumber);
							}
						}
						dt.Rows.Add(row);

						if (batchRows == rowsInBatch) {
							batchRows = 0;
							bulk.WriteToServer(dt);
							dt.Clear();
						}
					}
				}
				bulk.WriteToServer(dt);
				bulk.Close();
			}
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
				case "binary":
				case "varbinary":
				case "image":
					return SoapHexBinary.Parse(val).Value;
				default:
					return val;
			}
		}
	}
}
