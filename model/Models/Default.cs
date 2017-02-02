namespace SchemaZen.Library.Models {
	public class Default {
		public string Name;
		public string Value;

		public Default(string name, string value) {
			Name = name;
			Value = value;
		}

		public string ScriptAsPartOfColumnDefinition() {
			return $"CONSTRAINT [{Name}] DEFAULT {Value}";
		}

		public string ScriptDrop() {
			return $"DROP CONSTRAINT [{Name}]";
		}

		public string ScriptCreate(Column column) {
			return $"ADD {ScriptAsPartOfColumnDefinition()} FOR [{column.Name}]";
		}
	}
}
