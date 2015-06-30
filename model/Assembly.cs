using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace SchemaZen.model {
	public class SqlAssembly {
		public List<KeyValuePair<string, byte[]>> Files = new List<KeyValuePair<string, byte[]>>();
		public string Name;
		public string PermissionSet;

		public SqlAssembly(string permissionSet, string name) {
			PermissionSet = permissionSet;
			Name = name;

			if (PermissionSet == "SAFE_ACCESS")
				PermissionSet = "SAFE";
		}

		public string ScriptCreate(Database db) {
			IEnumerable<string> commands =
				Files.Select(
					(kvp, index) =>
						string.Format("{0} ASSEMBLY [{1}]\r\n{2}FROM {3}\r\n{4}", index == 0 ? "CREATE" : "ALTER", Name,
							index == 0 ? string.Empty : "ADD FILE ", "0x" + new SoapHexBinary(kvp.Value).ToString(),
							index == 0 ? "WITH PERMISSION_SET = " + PermissionSet : string.Format("AS N'{0}'", kvp.Key)));
			string script = string.Join("\r\nGO\r\n", commands.ToArray());
			return script;
		}

		public string ScriptDrop() {
			return string.Format("DROP {0} [{1}]", "ASSEMBLY", Name);
		}
	}
}
