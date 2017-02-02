﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;

namespace SchemaZen.Library.Models {
	public class Schema : INameable, IHasOwner, IScriptable {
		public string Name { get; set; }
		public string Owner { get; set; }

		public Schema(string name, string owner) {
			Owner = owner;
			Name = name;
		}

		public string ScriptCreate() {
			return $@"
if not exists(select s.schema_id from sys.schemas s where s.name = '{Name}') 
	and exists(select p.principal_id from sys.database_principals p where p.name = '{Owner}') begin
	exec sp_executesql N'create schema [{Name}] authorization [{Owner}]'
end
";
		}
	}

	public class Table : INameable, IHasOwner, IScriptable {
		private const string _fieldSeparator = "\t";
		private const string _escapeFieldSeparator = "--SchemaZenFieldSeparator--";
		private const string _rowSeparator = "\r\n";
		private const string _escapeRowSeparator = "--SchemaZenRowSeparator--";
		private const string _nullValue = "--SchemaZenNull--";
		public const int RowsInBatch = 15000;

		public ColumnList Columns = new ColumnList();
		private readonly List<Constraint> _constraints = new List<Constraint>();
		public string Name { get; set; }
		public string Owner { get; set; }
		public bool IsType;

		public Table(string owner, string name) {
			Owner = owner;
			Name = name;
		}

		public Constraint PrimaryKey {
			get { return _constraints.FirstOrDefault(c => c.Type == "PRIMARY KEY"); }
		}

		public Constraint FindConstraint(string name) {
			return _constraints.FirstOrDefault(c => c.Name == name);
		}

		public IEnumerable<Constraint> Constraints => _constraints.AsEnumerable();

		public void AddConstraint(Constraint constraint) {
			constraint.Table = this;
			_constraints.Add(constraint);
		}

		public void RemoveContraint(Constraint constraint) {
			_constraints.Remove(constraint);
		}

		public TableDiff Compare(Table t) {
			var diff = new TableDiff {
				Owner = t.Owner,
				Name = t.Name
			};

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

			if (!t.IsType) {
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
			} else {
				// compare constraints on table types, which can't be named in the script, but have names in the DB
				var dest = Constraints.ToList();
				var src = t.Constraints.ToList();

				var j = from c1 in dest
						join c2 in src on c1.ScriptCreate() equals c2.ScriptCreate() into match //new { c1.Type, c1.Unique, c1.Clustered, Columns = string.Join(",", c1.Columns.ToArray()), IncludedColumns = string.Join(",", c1.IncludedColumns.ToArray()) } equals new { c2.Type, c2.Unique, c2.Clustered, Columns = string.Join(",", c2.Columns.ToArray()), IncludedColumns = string.Join(",", c2.IncludedColumns.ToArray()) } into match
						from m in match.DefaultIfEmpty()
						select new { c1, m };

				foreach (var c in j) {
					if (c.m == null) {
						diff.ConstraintsAdded.Add(c.c1);
					} else {
						src.Remove(c.m);
					}
				}
				foreach (var c in src) {
					diff.ConstraintsDeleted.Add(c);
				}
			}

			return diff;
		}

		public string ScriptCreate() {
			var text = new StringBuilder();
			text.Append($"CREATE {(IsType ? "TYPE" : "TABLE")} [{Owner}].[{Name}] {(IsType ? "AS TABLE " : string.Empty)}(\r\n");
			text.Append(Columns.Script());
			if (_constraints.Count > 0) text.AppendLine();
			foreach (var c in _constraints.OrderBy(x => x.Name).Where(c => c.Type != "INDEX")) {
				text.AppendLine("   ," + c.ScriptCreate());
			}
			text.AppendLine(")");
			text.AppendLine();
			foreach (var c in _constraints.Where(c => c.Type == "INDEX")) {
				text.AppendLine(c.ScriptCreate());
			}
			return text.ToString();
		}

		public string ScriptDrop() {
			return $"DROP {(IsType ? "TYPE" : "TABLE")} [{Owner}].[{Name}]";
		}


		public void ExportData(string conn, TextWriter data, string tableHint = null) {
			if (IsType)
				throw new InvalidOperationException();

			var sql = new StringBuilder();
			sql.Append("select ");
			var cols = Columns.Items.Where(c => string.IsNullOrEmpty(c.ComputedDefinition)).ToArray();
			foreach (var c in cols) {
				sql.Append($"[{c.Name}],");
			}
			sql.Remove(sql.Length - 1, 1);
			sql.Append($" from [{Owner}].[{Name}]");
			if (!string.IsNullOrEmpty(tableHint))
				sql.Append($" WITH ({tableHint})");
			using (var cn = new SqlConnection(conn)) {
				cn.Open();
				using (var cm = cn.CreateCommand()) {
					cm.CommandText = sql.ToString();
					using (var dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							foreach (var c in cols) {
								if (dr[c.Name] is DBNull)
									data.Write(_nullValue);
								else if (dr[c.Name] is byte[])
									data.Write(new SoapHexBinary((byte[])dr[c.Name]).ToString());
								else
									data.Write(dr[c.Name].ToString()
										.Replace(_fieldSeparator, _escapeFieldSeparator)
										.Replace(_rowSeparator, _escapeRowSeparator));
								if (c != cols.Last())
									data.Write(_fieldSeparator);
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
			var batch_rows = 0;
			using (var bulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.TableLock)) {
				foreach (var colName in dt.Columns.OfType<DataColumn>().Select(c => c.ColumnName))
					bulk.ColumnMappings.Add(colName, colName);
				bulk.DestinationTableName = $"[{Owner}].[{Name}]";

				using (var file = new StreamReader(filename)) {
					var line = new List<char>();
					while (file.Peek() >= 0) {
						var rowsep_cnt = 0;
						line.Clear();

						while (file.Peek() >= 0) {
							var ch = (char)file.Read();
							line.Add(ch);

							if (ch == _rowSeparator[rowsep_cnt])
								rowsep_cnt++;
							else
								rowsep_cnt = 0;

							if (rowsep_cnt == _rowSeparator.Length) {
								// Remove rowseparator from line
								line.RemoveRange(line.Count - _rowSeparator.Length, _rowSeparator.Length);
								break;
							}
						}
						linenumber++;

						// Skip empty lines
						if (line.Count == 0)
							continue;

						batch_rows++;

						var row = dt.NewRow();
						var fields = (new String(line.ToArray())).Split(new[] { _fieldSeparator }, StringSplitOptions.None);
						if (fields.Length != dt.Columns.Count) {
							throw new DataFileException("Incorrect number of columns", filename, linenumber);
						}
						for (var j = 0; j < fields.Length; j++) {
							try {
								row[j] = ConvertType(cols[j].Type,
									fields[j].Replace(_escapeRowSeparator, _rowSeparator).Replace(_escapeFieldSeparator, _fieldSeparator));
							} catch (FormatException ex) {
								throw new DataFileException($"{ex.Message} at column {j + 1}", filename, linenumber);
							}
						}
						dt.Rows.Add(row);

						if (batch_rows == RowsInBatch) {
							batch_rows = 0;
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
			if (val == _nullValue)
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

	public class TableDiff {
		public List<Column> ColumnsAdded = new List<Column>();
		public List<ColumnDiff> ColumnsDiff = new List<ColumnDiff>();
		public List<Column> ColumnsDropped = new List<Column>();

		public List<Constraint> ConstraintsAdded = new List<Constraint>();
		public List<Constraint> ConstraintsChanged = new List<Constraint>();
		public List<Constraint> ConstraintsDeleted = new List<Constraint>();
		public string Name;
		public string Owner;

		public bool IsDiff => ColumnsAdded.Count + ColumnsDropped.Count + ColumnsDiff.Count + ConstraintsAdded.Count +
							  ConstraintsChanged.Count + ConstraintsDeleted.Count > 0;

		public string Script() {
			var text = new StringBuilder();

			foreach (var c in ColumnsAdded) {
				text.Append($"ALTER TABLE [{Owner}].[{Name}] ADD {c.ScriptCreate()}\r\n");
			}

			foreach (var c in ColumnsDropped) {
				text.Append($"ALTER TABLE [{Owner}].[{Name}] DROP COLUMN [{c.Name}]\r\n");
			}

			foreach (var c in ColumnsDiff) {
				if (c.DefaultIsDiff) {
					if (c.Source.Default != null) {
						text.Append($"ALTER TABLE [{Owner}].[{Name}] {c.Source.Default.ScriptDrop()}\r\n");
					}
					if (c.Target.Default != null) {
						text.Append($"ALTER TABLE [{Owner}].[{Name}] {c.Target.Default.ScriptCreate(c.Target)}\r\n");
					}
				}
				if (!c.OnlyDefaultIsDiff) {
					text.Append($"ALTER TABLE [{Owner}].[{Name}] ALTER COLUMN {c.Target.ScriptAlter()}\r\n");
				}
			}

			foreach (var c in ConstraintsAdded.Where(c => c.Type == "CHECK")) {
				text.Append($"ALTER TABLE [{Owner}].[{Name}] ADD {c.ScriptCreate()}\r\n");
			}

			foreach (var c in ConstraintsChanged.Where(c => c.Type == "CHECK")) {
				text.Append($"-- Check constraint {c.Name} changed\r\n");
				text.Append($"ALTER TABLE [{Owner}].[{Name}] DROP CONSTRAINT {c.Name}\r\n");
				text.Append($"ALTER TABLE [{Owner}].[{Name}] ADD {c.ScriptCreate()}\r\n");
			}

			foreach (var c in ConstraintsDeleted.Where(c => c.Type == "CHECK")) {
				text.Append($"ALTER TABLE [{Owner}].[{Name}] DROP CONSTRAINT {c.Name}\r\n");
			}

			return text.ToString();
		}
	}
}
