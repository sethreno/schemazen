using System;
using System.Collections.Generic;
using System.Text;

namespace model
{
	public class ForeignKey
	{
		public bool Check;
		public List<string> Columns = new List<string>();
		public string Name;
		public string OnDelete;
		public string OnUpdate;
		public List<string> RefColumns = new List<string>();
		public Table RefTable;
		public Table Table;

		public ForeignKey(string name)
		{
			this.Name = name;
		}

		public ForeignKey(Table table, string name, string columns, Table refTable, string refColumns)
			: this(table, name, columns, refTable, refColumns, "", "")
		{
		}

		public ForeignKey(Table table, string name, string columns, Table refTable, string refColumns, string onUpdate,
			string onDelete)
		{
			this.Table = table;
			this.Name = name;
			this.Columns = new List<string>(columns.Split(','));
			this.RefTable = refTable;
			this.RefColumns = new List<string>(refColumns.Split(','));
			this.OnUpdate = onUpdate;
			this.OnDelete = onDelete;
		}

		public string CheckText
		{
			get { return this.Check ? "CHECK" : "NOCHECK"; }
		}

		private void AssertArgNotNull(object arg, string argName)
		{
			if (arg == null)
			{
				throw new ArgumentNullException(string.Format(
					"Unable to Script FK {0}. {1} must not be null.", this.Name, argName));
			}
		}

		public string ScriptCreate()
		{
			this.AssertArgNotNull(this.Table, "Table");
			this.AssertArgNotNull(this.Columns, "Columns");
			this.AssertArgNotNull(this.RefTable, "RefTable");
			this.AssertArgNotNull(this.RefColumns, "RefColumns");

			var text = new StringBuilder();
			text.AppendFormat("ALTER TABLE [{0}].[{1}] WITH {2} ADD CONSTRAINT [{3}]\r\n", this.Table.Owner, this.Table.Name, this.CheckText, this.Name);
			text.AppendFormat("   FOREIGN KEY([{0}]) REFERENCES [{1}].[{2}] ([{3}])\r\n", string.Join("], [", this.Columns.ToArray()), this.RefTable.Owner, this.RefTable.Name, string.Join("], [", this.RefColumns.ToArray()));
			if (!string.IsNullOrEmpty(this.OnUpdate))
			{
				text.AppendFormat("   ON UPDATE {0}\r\n", this.OnUpdate);
			}
			if (!string.IsNullOrEmpty(this.OnDelete))
			{
				text.AppendFormat("   ON DELETE {0}\r\n", this.OnDelete);
			}
			if (!this.Check)
			{
				text.AppendFormat("   ALTER TABLE [{0}].[{1}] NOCHECK CONSTRAINT [{2}]\r\n", this.Table.Owner, this.Table.Name, this.Name);
			}
			return text.ToString();
		}

		public string ScriptDrop()
		{
			return string.Format("ALTER TABLE [{0}].[{1}] DROP CONSTRAINT [{2}]\r\n", this.Table.Owner, this.Table.Name, this.Name);
		}
	}
}