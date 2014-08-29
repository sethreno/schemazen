using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;

namespace model {
	public class ForeignKey {
		[XmlAttribute]
		public string Name;
		[XmlAttribute]
		[DefaultValue(false)]
		public bool Check;
		[XmlAttribute]
		public string OnDelete;
		[XmlAttribute]
		public string OnUpdate;

		[XmlArrayItem("Column")]
		public List<string> Columns = new List<string>();
		[XmlArrayItem("Column")]
		public List<string> RefColumns = new List<string>();
		public TableInfo RefTable;
		public TableInfo Table;

		private ForeignKey() { }

		public ForeignKey(string name) {
			Name = name;
		}

		public ForeignKey(Table table, string name, string columns, Table refTable, string refColumns)
			: this(table, name, columns, refTable, refColumns, "", "") {
		}

		public ForeignKey(Table table, string name, string columns, Table refTable, string refColumns, string onUpdate,
			string onDelete) {
			Table = new TableInfo(table.Owner, table.Name);
			Name = name;
			Columns = new List<string>(columns.Split(','));
			RefTable = new TableInfo(refTable.Owner, refTable.Name);
			RefColumns = new List<string>(refColumns.Split(','));
			OnUpdate = onUpdate;
			OnDelete = onDelete;
		}

		public string CheckText {
			get { return Check ? "CHECK" : "NOCHECK"; }
		}

		private void AssertArgNotNull(object arg, string argName) {
			if (arg == null) {
				throw new ArgumentNullException(String.Format(
					"Unable to Script FK {0}. {1} must not be null.",
					Name, argName));
			}
		}

		public string ScriptCreate() {
			AssertArgNotNull(Table, "Table");
			AssertArgNotNull(Columns, "Columns");
			AssertArgNotNull(RefTable, "RefTable");
			AssertArgNotNull(RefColumns, "RefColumns");

			var text = new StringBuilder();
			text.AppendFormat("ALTER TABLE [{0}].[{1}] WITH {2} ADD CONSTRAINT [{3}]\r\n", Table.Owner, Table.Name, CheckText,
				Name);
			text.AppendFormat("   FOREIGN KEY([{0}]) REFERENCES [{1}].[{2}] ([{3}])\r\n", string.Join("], [", Columns.ToArray()),
				RefTable.Owner, RefTable.Name, string.Join("], [", RefColumns.ToArray()));
			if (!string.IsNullOrEmpty(OnUpdate)) {
				text.AppendFormat("   ON UPDATE {0}\r\n", OnUpdate);
			}
			if (!string.IsNullOrEmpty(OnDelete)) {
				text.AppendFormat("   ON DELETE {0}\r\n", OnDelete);
			}
			if (!Check) {
				text.AppendFormat("   ALTER TABLE [{0}].[{1}] NOCHECK CONSTRAINT [{2}]\r\n",
					Table.Owner, Table.Name, Name);
			}
			return text.ToString();
		}

		public string ScriptDrop() {
			return string.Format("ALTER TABLE [{0}].[{1}] DROP CONSTRAINT [{2}]\r\n", Table.Owner, Table.Name, Name);
		}
	}
}