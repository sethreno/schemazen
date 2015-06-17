using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;

namespace model
{
	public class Schema
	{
		public string Name;
		public string Owner;

		public Schema(string name, string owner)
		{
			this.Owner = owner;
			this.Name = name;
		}
	}

	public class Table
	{
		public ColumnList Columns = new ColumnList();
		public List<Constraint> Constraints = new List<Constraint>();
		public string Name;
		public string Owner;

		public Table(string owner, string name)
		{
			this.Owner = owner;
			this.Name = name;
		}

		public Constraint PrimaryKey
		{
			get
			{
				return this.Constraints.FirstOrDefault(c => c.Type == "PRIMARY KEY");
			}
		}

		public Constraint FindConstraint(string name)
		{
			return this.Constraints.FirstOrDefault(c => c.Name == name);
		}

		public TableDiff Compare(Table t)
		{
			var diff = new TableDiff();
			diff.Owner = t.Owner;
			diff.Name = t.Name;

			//get additions and compare mutual columns
			foreach (var c in this.Columns.Items)
			{
				var c2 = t.Columns.Find(c.Name);
				if (c2 == null)
				{
					diff.ColumnsAdded.Add(c);
				}
				else
				{
					//compare mutual columns
					var cDiff = c.Compare(c2);
					if (cDiff.IsDiff)
					{
						diff.ColumnsDiff.Add(cDiff);
					}
				}
			}

			//get deletions
			foreach (var c in t.Columns.Items.Where(c => this.Columns.Find(c.Name) == null))
			{
				diff.ColumnsDropped.Add(c);
			}

			//get added and compare mutual constraints
			foreach (var c in this.Constraints)
			{
				var c2 = t.FindConstraint(c.Name);
				if (c2 == null)
				{
					diff.ConstraintsAdded.Add(c);
				}
				else
				{
					if (c.Script() != c2.Script())
					{
						diff.ConstraintsChanged.Add(c);
					}
				}
			}
			//get deleted constraints
			foreach (var c in t.Constraints.Where(c => this.FindConstraint(c.Name) == null))
			{
				diff.ConstraintsDeleted.Add(c);
			}

			return diff;
		}

		public string ScriptCreate()
		{
			var text = new StringBuilder();
			text.AppendFormat("CREATE TABLE [{0}].[{1}](\r\n", this.Owner, this.Name);
			text.Append(this.Columns.Script());
			if (this.Constraints.Count > 0) text.AppendLine();
			foreach (var c in this.Constraints.Where(c => c.Type != "INDEX"))
			{
				text.AppendLine("   ," + c.Script());
			}
			text.AppendLine(")");
			text.AppendLine();
			foreach (var c in this.Constraints.Where(c => c.Type == "INDEX"))
			{
				text.AppendLine(c.Script());
			}
			return text.ToString();
		}

		public string ScriptDrop()
		{
			return string.Format("DROP TABLE [{0}].[{1}]", this.Owner, this.Name);
		}


		private const string fieldSeparator = "\t";
		private const string escapeFieldSeparator = "--SchemaZenFieldSeparator--";
		private const string rowSeparator = "\r\n";
		private const string escapeRowSeparator = "--SchemaZenRowSeparator--";
		private const string nullValue = "--SchemaZenNull--";

		public string ExportData(string conn)
		{
			var data = new StringBuilder(); // TODO: better to use a StringWriter... saves having to store the whole thing in memory first before writing to a file
			var sql = new StringBuilder();
			sql.Append("select ");
			foreach (var c in this.Columns.Items)
			{
				sql.AppendFormat("[{0}],", c.Name);
			}
			sql.Remove(sql.Length - 1, 1);
			sql.AppendFormat(" from [{0}].[{1}]", this.Owner, this.Name);
			using (var cn = new SqlConnection(conn))
			{
				cn.Open();
				using (var cm = cn.CreateCommand())
				{
					cm.CommandText = sql.ToString();
					using (var dr = cm.ExecuteReader())
					{
						while (dr.Read())
						{
							foreach (var c in this.Columns.Items)
							{
								if (dr[c.Name] is DBNull)
									data.Append(nullValue);
								else if (dr[c.Name] is byte[])
									data.Append(new SoapHexBinary((byte[])dr[c.Name]).ToString());
								else
									data.Append(dr[c.Name].ToString().Replace(fieldSeparator, escapeFieldSeparator).Replace(rowSeparator, escapeRowSeparator));
								data.Append(fieldSeparator);
							}
							data.Remove(data.Length - fieldSeparator.Length, fieldSeparator.Length);
							data.AppendLine();
						}
					}
				}
			}

			return data.ToString();
		}

		public void ImportData(string conn, string data)
		{
			var dt = new DataTable();
			foreach (var c in this.Columns.Items)
			{
				dt.Columns.Add(new DataColumn(c.Name, SqlTypeToNativeType(c.Type)));
			}
			var lines = data.Split(new[] { rowSeparator }, StringSplitOptions.RemoveEmptyEntries);
			var i = 0;
			foreach (var line in lines)
			{
				i++;
				var row = dt.NewRow();
				var fields = line.Split(new[] { fieldSeparator }, StringSplitOptions.None);
				if (fields.Length != this.Columns.Items.Count)
				{
					throw new DataException("Incorrect number of columns", i);
				}
				for (var j = 0; j < fields.Length; j++)
				{
					try
					{
						row[j] = this.ConvertType(this.Columns.Items[j].Type, fields[j].Replace(escapeRowSeparator, rowSeparator).Replace(escapeFieldSeparator, fieldSeparator));
					}
					catch (FormatException ex)
					{
						throw new DataException(string.Format("{0} at column {1}", ex.Message, j + 1), i);
					}
				}
				dt.Rows.Add(row);
			}

			var bulk = new SqlBulkCopy(conn,
				SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.TableLock);
			bulk.DestinationTableName = this.Name;
			bulk.WriteToServer(dt);
		}

		private static Type SqlTypeToNativeType(string sqlType)
		{
			switch (sqlType.ToLower())
			{
				case "bit":
					return typeof(bool);
				case "datetime":
				case "smalldatetime":
					return typeof(DateTime);
				case "int":
					return typeof(int);
				case "uniqueidentifier":
					return typeof(Guid);
				case "varbinary":
					return typeof(byte[]);
				default:
					return typeof(string);
			}
		}

		public object ConvertType(string sqlType, string val)
		{
			if (val == nullValue)
				return DBNull.Value;

			switch (sqlType.ToLower())
			{
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

	public class TableDiff
	{
		public List<Column> ColumnsAdded = new List<Column>();
		public List<ColumnDiff> ColumnsDiff = new List<ColumnDiff>();
		public List<Column> ColumnsDropped = new List<Column>();

		public List<Constraint> ConstraintsAdded = new List<Constraint>();
		public List<Constraint> ConstraintsChanged = new List<Constraint>();
		public List<Constraint> ConstraintsDeleted = new List<Constraint>();
		public string Name;
		public string Owner;

		public bool IsDiff
		{
			get
			{
				return this.ColumnsAdded.Count + this.ColumnsDropped.Count + this.ColumnsDiff.Count + this.ConstraintsAdded.Count + this.ConstraintsChanged.Count + this.ConstraintsDeleted.Count > 0;
			}
		}

		public string Script()
		{
			var text = new StringBuilder();

			foreach (var c in this.ColumnsAdded)
			{
				text.AppendFormat("ALTER TABLE [{0}].[{1}] ADD {2}\r\n", this.Owner, this.Name, c.Script());
			}

			foreach (var c in this.ColumnsDropped)
			{
				text.AppendFormat("ALTER TABLE [{0}].[{1}] DROP COLUMN [{2}]\r\n", this.Owner, this.Name, c.Name);
			}

			foreach (var c in this.ColumnsDiff)
			{
				text.AppendFormat("ALTER TABLE [{0}].[{1}] ALTER COLUMN {2}\r\n", this.Owner, this.Name, c.Script());
			}
			return text.ToString();
		}
	}
}