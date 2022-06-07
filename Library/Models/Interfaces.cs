namespace SchemaZen.Library.Models;

public interface INameable {
	string Name { get; set; }
}

public interface IHasOwner {
	string Owner { get; set; }
}

public interface IScriptable {
	string ScriptCreate();
}
