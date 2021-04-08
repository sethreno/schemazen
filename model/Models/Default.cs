namespace SchemaZen.Library.Models {
	public class Default : IScriptable, INameable {
		public string Name { get; set; }
		public string Value;
		public bool IsSystemNamed;
		public Column Column;
		public Table Table;

		public bool ScriptInline { get { return Table == null || (Table != null && Table.IsType); } }

		public Default(string name, string value, bool isSystemNamed) {
			Name = name;
			Value = value;
			IsSystemNamed = isSystemNamed;
		}

		public Default(Table table, Column column, string name, string value, bool isSystemNamed) {
			Name = name;
			Value = value;
			IsSystemNamed = isSystemNamed;
			Column = column;
			Table = table;
		}

		private string ScriptAsPartOfColumnDefinition() {
			return IsSystemNamed ? $" DEFAULT {Value}" : $"CONSTRAINT [{Name}] DEFAULT {Value}";
		}

		public string ScriptDrop() {
			return $"DROP CONSTRAINT [{Name}]";
		}

		public string ScriptCreate() {
			if (ScriptInline) {
				return ScriptAsPartOfColumnDefinition();
			} else {
				return ScriptCreate(Column);
			}
		}

		public string ScriptCreate(Column column) {
			return $"ALTER TABLE [{Table.Owner}].[{Table.Name}] ADD {ScriptAsPartOfColumnDefinition()} FOR [{column.Name}]";
		}
	}
}
