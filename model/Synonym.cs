namespace SchemaZen.model {

	public class Synonym : INameable, IHasOwner, IScriptable {
		public string Name { get; set; }
		public string Owner { get; set; }
		public string BaseObjectName;

		public Synonym(string name, string owner) {
			Name = name;
			Owner = owner;
		}

		public string ScriptCreate() {
			return string.Format("CREATE SYNONYM [{0}].[{1}] FOR {2}", Owner, Name, BaseObjectName);
		}

		public string ScriptDrop() {
			return string.Format("DROP SYNONYM [{0}].[{1}]", Owner, Name);
		}
	}
}
