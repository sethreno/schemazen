using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
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
		private const string _rowSeparator = "\r\n";
		private const string _tab = "\t";
		private const string _escapeTab = "--SchemaZenTAB--";
		private const string _carriageReturn = "\r";
		private const string _escapeCarriageReturn = "--SchemaZenCR--";
		private const string _lineFeed = "\n";
		private const string _escapeLineFeed = "--SchemaZenLF--";
		private const string _nullValue = "--SchemaZenNull--";
		private const string _dateTimeFormat = "yyyy-MM-dd HH:mm:ss.FFFFFFF";

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
					join c2 in src on c1.ScriptCreate() equals c2.ScriptCreate() into
						match //new { c1.Type, c1.Unique, c1.Clustered, Columns = string.Join(",", c1.Columns.ToArray()), IncludedColumns = string.Join(",", c1.IncludedColumns.ToArray()) } equals new { c2.Type, c2.Unique, c2.Clustered, Columns = string.Join(",", c2.Columns.ToArray()), IncludedColumns = string.Join(",", c2.IncludedColumns.ToArray()) } into match
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
			text.Append(
				$"CREATE {(IsType ? "TYPE" : "TABLE")} [{Owner}].[{Name}] {(IsType ? "AS TABLE " : string.Empty)}(\r\n");
			text.Append(Columns.Script());
			if (_constraints.Count > 0) text.AppendLine();
			if (!IsType) {
				foreach (var c in _constraints.OrderBy(x => x.Name).Where(c => c.Type == "PRIMARY KEY" || c.Type == "UNIQUE")) {
					text.AppendLine("   ," + c.ScriptCreate());
				}
			} else {
				foreach (var c in _constraints.OrderBy(x => x.Name).Where(c => c.Type != "INDEX")) {
					text.AppendLine("   ," + c.ScriptCreate());
				}
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
			var cols = Columns.Items.Where(c => string.IsNullOrEmpty(c.ComputedDefinition))
				.ToArray();
			foreach (var c in cols) {
				sql.Append($"[{c.Name}],");
			}

			sql.Remove(sql.Length - 1, 1);
			sql.Append($" from [{Owner}].[{Name}]");
			if (!string.IsNullOrEmpty(tableHint))
				sql.Append($" WITH ({tableHint})");

			AppendOrderBy(sql, cols);

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
									data.Write(Convert.ToHexString((byte[])dr[c.Name]));
								else if (dr[c.Name] is DateTime) {
									data.Write(
										((DateTime)dr[c.Name])
										.ToString(_dateTimeFormat, CultureInfo.InvariantCulture));
								} else if (dr[c.Name] is float || dr[c.Name] is Double ||
									dr[c.Name] is Decimal) {
									data.Write(Convert.ToString(dr[c.Name],
										CultureInfo.InvariantCulture));
								} else {
									data.Write(dr[c.Name].ToString()
										.Replace(_tab, _escapeTab)
										.Replace(_lineFeed, _escapeLineFeed)
										.Replace(_carriageReturn, _escapeCarriageReturn));
								}

								if (c != cols.Last())
									data.Write(_tab);
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
			var cols = Columns.Items.Where(c => string.IsNullOrEmpty(c.ComputedDefinition))
				.ToArray();
			foreach (var c in cols) {
				dt.Columns.Add(new DataColumn(c.Name, c.SqlTypeToNativeType()));
			}

			var linenumber = 0;
			var batch_rows = 0;
			using (var bulk = new SqlBulkCopy(conn,
				SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.KeepNulls |
				SqlBulkCopyOptions.TableLock)) {
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
								line.RemoveRange(line.Count - _rowSeparator.Length,
									_rowSeparator.Length);
								break;
							}
						}

						linenumber++;

						// Skip empty lines
						if (line.Count == 0)
							continue;

						batch_rows++;

						var row = dt.NewRow();
						var fields =
							new String(line.ToArray()).Split(new[] { _tab },
								StringSplitOptions.None);
						if (fields.Length != dt.Columns.Count) {
							throw new DataFileException("Incorrect number of columns", filename,
								linenumber);
						}

						for (var j = 0; j < fields.Length; j++) {
							try {
								row[j] = ConvertType(cols[j].Type,
									fields[j].Replace(_escapeLineFeed, _lineFeed)
										.Replace(_escapeCarriageReturn, _carriageReturn)
										.Replace(_escapeTab, _tab));
							} catch (FormatException ex) {
								throw new DataFileException($"{ex.Message} at column {j + 1}",
									filename, linenumber);
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
					return DateTime.Parse(val, CultureInfo.InvariantCulture);
				case "int":
					return int.Parse(val);
				case "float":
					return double.Parse(val, CultureInfo.InvariantCulture);
				case "decimal":
					return decimal.Parse(val, CultureInfo.InvariantCulture);
				case "uniqueidentifier":
					return new Guid(val);
				case "binary":
				case "varbinary":
				case "image":
					return Convert.FromHexString(val);
				default:
					return val;
			}
		}

		private void AppendOrderBy(StringBuilder sql, IEnumerable<Column> cols) {
			sql.Append(" ORDER BY ");

			if (PrimaryKey != null) {
				var pkColumns = PrimaryKey.Columns.Select(c => $"[{c.ColumnName}]");
				sql.Append(string.Join(",", pkColumns.ToArray()));
				return;
			}

			var uk = Constraints.Where(c => c.Unique).OrderBy(c => c.Columns.Count)
				.ThenBy(c => c.Name).FirstOrDefault();

			if (uk != null) {
				var ukColumns = uk.Columns.Select(c => $"[{c.ColumnName}]");
				sql.Append(string.Join(",", ukColumns.ToArray()));
				return;
			}

			var allColumns = cols.Select(c => $"[{c.Name}]");
			sql.Append(string.Join(",", allColumns.ToArray()));
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

		public bool IsDiff => ColumnsAdded.Count + ColumnsDropped.Count + ColumnsDiff.Count +
			ConstraintsAdded.Count +
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
						text.Append(
							$"ALTER TABLE [{Owner}].[{Name}] {c.Source.Default.ScriptDrop()}\r\n");
					}

					if (c.Target.Default != null) {
						text.Append(
							$"ALTER TABLE [{Owner}].[{Name}] {c.Target.Default.ScriptCreate(c.Target)}\r\n");
					}
				}

				if (!c.OnlyDefaultIsDiff) {
					text.Append(
						$"ALTER TABLE [{Owner}].[{Name}] ALTER COLUMN {c.Target.ScriptAlter()}\r\n");
				}
			}

			void ScriptUnspported(Constraint c) {
				text.AppendLine("-- constraint added that SchemaZen doesn't support yet");
				text.AppendLine("/*");
				text.AppendLine(c.ScriptCreate());
				text.AppendLine("*/");
			}

			foreach (var c in ConstraintsAdded) {
				switch (c.Type) {
					case "CHECK":
					case "INDEX":
						text.Append($"ALTER TABLE [{Owner}].[{Name}] ADD {c.ScriptCreate()}\r\n");
						break;

					default:
						ScriptUnspported(c);
						break;
				}
			}

			foreach (var c in ConstraintsChanged) {
				switch (c.Type) {
					case "CHECK":
						text.Append($"-- Check constraint {c.Name} changed\r\n");
						text.Append($"ALTER TABLE [{Owner}].[{Name}] DROP CONSTRAINT {c.Name}\r\n");
						text.Append($"ALTER TABLE [{Owner}].[{Name}] ADD {c.ScriptCreate()}\r\n");
						break;

					case "INDEX":
						text.Append($"-- Index {c.Name} changed\r\n");
						text.Append($"DROP INDEX {c.Name} ON [{Owner}].[{Name}]\r\n");
						text.Append($"{c.ScriptCreate()}\r\n");
						break;

					default:
						ScriptUnspported(c);
						break;
				}
			}

			foreach (var c in ConstraintsDeleted) {
				text.Append(c.Type != "INDEX" ?
					$"ALTER TABLE [{Owner}].[{Name}] DROP CONSTRAINT {c.Name}\r\n" :
					$"DROP INDEX {c.Name} ON [{Owner}].[{Name}]\r\n");
			}

			return text.ToString();
		}
	}
}
