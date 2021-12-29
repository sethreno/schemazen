namespace SchemaZen.Library.Models;

public class Schema : INameable, IHasOwner, IScriptable {
	public Schema(string name, string owner) {
		Owner = owner;
		Name = name;
	}

	public string Owner { get; set; }
	public string Name { get; set; }

	public string ScriptCreate() {
		return $"create schema [{Name}] authorization [{Owner}]";
	}
}
