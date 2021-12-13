namespace SchemaZen.Library.Models;

public class Default : IScriptable, INameable {
	public Column Column;
	public bool IsSystemNamed;
	public Table Table;
	public string Value;

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

	public bool ScriptInline => Table == null || Table != null && Table.IsType;
	public string Name { get; set; }

	public string ScriptCreate() {
		if (ScriptInline)
			return ScriptAsPartOfColumnDefinition();
		return ScriptCreate(Column);
	}

	private string ScriptAsPartOfColumnDefinition() {
		return IsSystemNamed ? $" DEFAULT {Value}" : $"CONSTRAINT [{Name}] DEFAULT {Value}";
	}

	public string ScriptDrop() {
		return $"DROP CONSTRAINT [{Name}]";
	}

	public string ScriptCreate(Column column) {
		return
			$"ALTER TABLE [{Table.Owner}].[{Table.Name}] ADD {ScriptAsPartOfColumnDefinition()} FOR [{column.Name}]";
	}
}
