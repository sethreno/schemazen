using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace SchemaZen.Library.Models {
	public class SqlAssembly : INameable, IScriptable {
		public List<KeyValuePair<string, byte[]>> Files = new List<KeyValuePair<string, byte[]>>();
		public string Name { get; set; }
		public string PermissionSet;

		public SqlAssembly(string permissionSet, string name) {
			PermissionSet = permissionSet;
			Name = name;

			if (PermissionSet == "SAFE_ACCESS")
				PermissionSet = "SAFE";

			if (PermissionSet == "UNSAFE_ACCESS")
				PermissionSet = "UNSAFE";
		}

		public string ScriptCreate() {
			var commands = Files.Select((kvp, index) => {
				if (index == 0) {
					return $"CREATE ASSEMBLY [{Name}]\r\n{string.Empty}FROM {"0x" + new SoapHexBinary(kvp.Value).ToString()}\r\n{"WITH PERMISSION_SET = " + PermissionSet}";
				}
				return $"ALTER ASSEMBLY [{Name}]\r\nADD FILE FROM {"0x" + new SoapHexBinary(kvp.Value).ToString()}\r\nAS N\'{kvp.Key}\'";
			});

			var script = string.Join("\r\nGO\r\n", commands.ToArray());
			return script;
		}

		public string ScriptDrop() {
			return $"DROP ASSEMBLY [{Name}]";
		}
	}
}
