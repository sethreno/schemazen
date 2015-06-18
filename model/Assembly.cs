using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;

namespace model
{
	public class SqlAssembly
	{
		public string Name;
		public string PermissionSet;
		public List<KeyValuePair<string, byte[]>> Files = new List<KeyValuePair<string, byte[]>>();

		public SqlAssembly(string permissionSet, string name)
		{
			this.PermissionSet = permissionSet;
			this.Name = name;

			if (this.PermissionSet == "SAFE_ACCESS")
				this.PermissionSet = "SAFE";
		}

		public string ScriptCreate(Database db)
		{
			var commands = this.Files.Select((kvp, index) => string.Format("{0} ASSEMBLY [{1}]\r\n{2}FROM {3}\r\n{4}", index == 0 ? "CREATE" : "ALTER", this.Name, index == 0 ? string.Empty : "ADD FILE ", "0x" + new SoapHexBinary(kvp.Value).ToString(), index == 0 ? "WITH PERMISSION_SET = " + this.PermissionSet : string.Format("AS N'{0}'", kvp.Key)));
			var script = string.Join("\r\nGO\r\n", commands.ToArray());
			return script;
		}

		public string ScriptDrop()
		{
			return string.Format("DROP {0} [{1}]", "ASSEMBLY", this.Name);
		}
	}
}
