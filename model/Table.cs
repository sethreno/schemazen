using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace model {
	public class Schema
	{
		[XmlAttribute]
		public string Name;
		[XmlAttribute]
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

	public class Table : CompareBase, ITableInfo {
		private const string DefaultOwner = "dbo";

		[XmlAttribute]
		public string Name { get; set; }
		[XmlAttribute]
		[DefaultValue(DefaultOwner)]
		public string Owner { get; set; }

		public ColumnList Columns = new ColumnList();
		public List<Constraint> Constraints = new List<Constraint>();

		private Table() {
			Owner = DefaultOwner;
		}

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

		public Constraint FindSimilarConstraint(Constraint otherConstraint) {
			foreach (Constraint c in Constraints) {
				if (c.IsSimilar(otherConstraint)){
					return c;
				}
			}
			return null;
		}

		public TableDiff Compare(Table otherTable, CompareConfig compareConfig) {
			var diff = new TableDiff();
			diff.Owner = otherTable.Owner;
			diff.Name = otherTable.Name;

			CompareColumns(otherTable, compareConfig, diff);
			CompareConstraints(otherTable, compareConfig, diff);

			return diff;
		}

		private void CompareConstraints(Table otherTable, CompareConfig compareConfig, TableDiff diff) {
			Action<Constraint, Constraint> checkIfConstraintChanged = (c, c2) => {
				if(!c.HasSameProperties(c2, compareConfig)) {
				    diff.ConstraintsChanged.Add(c);
				};
			};

			Func<Constraint, Constraint> getOtherConstraint = (c) => {
				var c2 = otherTable.FindConstraint(c.Name);
				if(compareConfig.IgnoreConstraintsNameMismatch && c2 == null)
					return  otherTable.FindSimilarConstraint(c);

				return c2;
			};

			CheckSource(compareConfig.ConstraintsCompareMethod,
				Constraints,
				getOtherConstraint,
				c => diff.ConstraintsAdded.Add(c),
				checkIfConstraintChanged);

			Func<Constraint, bool> constraintExistsOnlyInTaget = (c) => {
				var c2 = FindConstraint(c.Name);
				if (compareConfig.IgnoreConstraintsNameMismatch && c2 == null)
					c2 = FindSimilarConstraint(c);

				return c2 == null;
			};
			CheckTarget(compareConfig.ConstraintsCompareMethod,
				otherTable.Constraints,
				constraintExistsOnlyInTaget,
				c => diff.ConstraintsDeleted.Add(c));
		}

		private void CompareColumns(Table otherTable, CompareConfig compareConfig, TableDiff diff) {
			Action<Column, Column> checkIfColumnChanged = (c, c2) => {
				//compare mutual columns
				ColumnDiff cDiff = c.Compare(c2, compareConfig);
				if (cDiff.IsDiff) {
					diff.ColumnsDiff.Add(cDiff);
				}
			};

			CheckSource(compareConfig.ColumnsCompareMethod,
				Columns,
				c => otherTable.Columns.Find(c.Name),
				c => diff.ColumnsAdded.Add(c),
				checkIfColumnChanged);

			CheckTarget(compareConfig.RoutinesCompareMethod,
				otherTable.Columns,
				c => Columns.Find(c.Name) == null,
				c => diff.ColumnsDroped.Add(c));
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
			foreach (Column c in Columns) {
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
							foreach (Column c in Columns) {
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
			foreach (Column c in Columns) {
				dt.Columns.Add(new DataColumn(c.Name));
			}
			string[] lines = data.Split("\r\n".Split(','), StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < lines.Count(); i++) {
				string line = lines[i];
				DataRow row = dt.NewRow();
				string[] fields = line.Split('\t');
				if (fields.Length != Columns.Count) {
					throw new DataException("Incorrect number of columns", i + 1);
				}
				for (int j = 0; j < fields.Length; j++) {
					try {
						row[j] = ConvertType(Columns[j].Type, fields[j]);
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