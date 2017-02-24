namespace SchemaZen.Library.Models {
	public class Default {
		public string Name;
		public string Value;
	    public bool IsSystemNamed;

	    public Default(string name, string value, bool isSystemNamed) {
			Name = name;
			Value = value;
	        IsSystemNamed = isSystemNamed;
	    }

		public string ScriptAsPartOfColumnDefinition() {
		    return IsSystemNamed ? $" DEFAULT {Value}" : $"CONSTRAINT [{Name}] DEFAULT {Value}";
		}

		public string ScriptDrop() {
			return $"DROP CONSTRAINT [{Name}]";
		}

		public string ScriptCreate(Column column) {
			return $"ADD {ScriptAsPartOfColumnDefinition()} FOR [{column.Name}]";
		}
	}
}
