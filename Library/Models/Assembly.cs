using System;
using System.Collections.Generic;
using System.Linq;

namespace SchemaZen.Library.Models;

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
				return $@"CREATE ASSEMBLY [{Name}]
{string.Empty}FROM {"0x" + Convert.ToHexString(kvp.Value)}
{"WITH PERMISSION_SET = " + PermissionSet}";
			}

			return
				$@"ALTER ASSEMBLY [{Name}]
ADD FILE FROM {"0x" + Convert.ToHexString(kvp.Value)}
AS N\'{kvp.Key}\'";
		});

		var go = Environment.NewLine + "GO" + Environment.NewLine;

		var script = string.Join(go, commands.ToArray());
		return script;
	}

	public string ScriptDrop() {
		return $"DROP ASSEMBLY [{Name}]";
	}
}
