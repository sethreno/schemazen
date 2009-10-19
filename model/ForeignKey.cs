using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace model {
	public class ForeignKey {
		public Table Table;
		public string Name;
		public List<string> Columns = new List<string>();
		public Table RefTable;
		public List<string> RefColumns = new List<string>();
		public string OnUpdate;
		public string OnDelete;

		public ForeignKey(string name) {
			this.Name = name;
		}

		public ForeignKey(Table table, string name, string columns, Table refTable, string refColumns)
			: this(table, name, columns, refTable, refColumns, "", "") {
		}

		public ForeignKey(Table table, string name, string columns, Table refTable, string refColumns, string onUpdate, string onDelete) {
			this.Table = table;
			this.Name = name;
			this.Columns = new List<string>(columns.Split(','));
			this.RefTable = refTable;
			this.RefColumns = new List<string>(refColumns.Split(','));
			this.OnUpdate = onUpdate;
			this.OnDelete = onDelete;
		}

		public string ScriptCreate() {
			StringBuilder text = new StringBuilder();
			text.AppendFormat("ALTER TABLE [{0}].[{1}] WITH CHECK ADD CONSTRAINT [{2}]\r\n", Table.Owner, Table.Name, Name);
			text.AppendFormat("   FOREIGN KEY([{0}]) REFERENCES [{1}].[{2}] ([{3}])\r\n", string.Join("], [", Columns.ToArray()), RefTable.Owner, RefTable.Name, string.Join("], [", RefColumns.ToArray()));
			if (!string.IsNullOrEmpty(OnUpdate)) {
				text.AppendFormat("   ON UPDATE {0}\r\n", OnUpdate);
			}
			if (!string.IsNullOrEmpty(OnDelete)) {
				text.AppendFormat("   ON DELETE {0}\r\n", OnDelete);
			}
			return text.ToString();
		}

		public string ScriptDrop() {
			return string.Format("ALTER TABLE [{0}].[{1}] DROP CONSTRAINT [{2}]", Table.Owner, Table.Name, Name);
		}
	}

}
