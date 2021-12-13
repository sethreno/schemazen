namespace SchemaZen.Library.Models;

public class Synonym : INameable, IHasOwner, IScriptable {
	public Synonym(string name, string owner) {
		Name = name;
		Owner = owner;
	}

	public string BaseObjectName { get; set; }
	public string Owner { get; set; }
	public string Name { get; set; }

	public string ScriptCreate() {
		return $"CREATE SYNONYM [{Owner}].[{Name}] FOR {BaseObjectName}";
	}

	public string ScriptDrop() {
		return $"DROP SYNONYM [{Owner}].[{Name}]";
	}
}
