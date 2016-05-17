namespace SchemaZen.Library.Models {
	public class Default {
		public string Name;
		public string Value;

		public Default(string name, string value) {
			Name = name;
			Value = value;
		}

		public string ScriptAsPartOfColumnDefinition() {
			return string.Format("CONSTRAINT [{0}] DEFAULT {1}", Name, Value);
		}

		public string ScriptDrop() {
			return string.Format("DROP CONSTRAINT [{0}]", Name);
		}

		public string ScriptCreate(Column column) {
			return string.Format("ADD {0} FOR [{1}]", ScriptAsPartOfColumnDefinition(), column.Name);
		}
	}
}
