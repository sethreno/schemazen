﻿namespace SchemaZen.model {
	public interface INameable : IScriptable {
		string Name { get; set; }
	}

	public interface IHasOwner {
		string Owner { get; set; }
	}

	public interface IScriptable {
		string ScriptCreate();
	}
}
