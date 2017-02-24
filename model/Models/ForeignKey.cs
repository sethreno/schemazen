using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SchemaZen.Library.Models {
	public class ForeignKey : INameable, IScriptable {
		public bool Check { get; set; }
        public bool IsSystemNamed { get; set; }
        public List<string> Columns { get; set; } = new List<string>();
		public string Name { get; set; }
		public string OnDelete { get; set; }
		public string OnUpdate { get; set; }
		public List<string> RefColumns { get; set; } = new List<string>();
		public Table RefTable { get; set; }
		public Table Table { get; set; }

		private const string _defaultRules = "NO ACTION|RESTRICT";

		public ForeignKey(string name) {
			Name = name;
		}

		public ForeignKey(Table table, string name, string columns, Table refTable, string refColumns)
			: this(table, name, columns, refTable, refColumns, "", "") { }

		public ForeignKey(Table table, string name, string columns, Table refTable, string refColumns, string onUpdate,
			string onDelete) {
			Table = table;
			Name = name;
			Columns = new List<string>(columns.Split(','));
			RefTable = refTable;
			RefColumns = new List<string>(refColumns.Split(','));
			OnUpdate = onUpdate;
			OnDelete = onDelete;
		}

		public string CheckText => Check ? "CHECK" : "NOCHECK";
	    
	    private void AssertArgNotNull(object arg, string argName) {
			if (arg == null) {
				throw new ArgumentNullException($"Unable to Script FK {Name} on table {Table.Owner}.{Table.Name}. {argName} must not be null.");
			}
		}

		public string ScriptCreate() {
			AssertArgNotNull(Table, "Table");
			AssertArgNotNull(Columns, "Columns");
			AssertArgNotNull(RefTable, "RefTable");
			AssertArgNotNull(RefColumns, "RefColumns");

			var text = new StringBuilder();
		    var constraintName = IsSystemNamed ? string.Empty : $"CONSTRAINT [{Name}]";
			text.Append($"ALTER TABLE [{Table.Owner}].[{Table.Name}] WITH {CheckText} ADD {constraintName}\r\n");
			text.Append($"   FOREIGN KEY([{string.Join("], [", Columns.ToArray())}]) REFERENCES [{RefTable.Owner}].[{RefTable.Name}] ([{string.Join("], [", RefColumns.ToArray())}])\r\n");
			if (!string.IsNullOrEmpty(OnUpdate) && !_defaultRules.Split('|').Contains(OnUpdate)) {
				text.Append($"   ON UPDATE {OnUpdate}\r\n");
			}
			if (!string.IsNullOrEmpty(OnDelete) && !_defaultRules.Split('|').Contains(OnDelete)) {
				text.Append($"   ON DELETE {OnDelete}\r\n");
			}
            if (!Check && !IsSystemNamed) {
                text.Append($"   ALTER TABLE [{Table.Owner}].[{Table.Name}] NOCHECK CONSTRAINT [{Name}]\r\n");
            }
            return text.ToString();
		}

		public string ScriptDrop() {
			return $"ALTER TABLE [{Table.Owner}].[{Table.Name}] DROP CONSTRAINT [{Name}]\r\n";
		}
	}
}
