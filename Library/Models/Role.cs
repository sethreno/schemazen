namespace SchemaZen.Library.Models;

public class Role : IScriptable, INameable {
	public string Name { get; set; }

	public string ScriptCreate() {
		return $"CREATE ROLE {Name}";
	}
}
