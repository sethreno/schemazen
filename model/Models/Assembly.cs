using System;
using System.Runtime;
using System.Collections.Generic;
using System.Linq;

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
					return
						$"CREATE ASSEMBLY [{Name}]\r\n{string.Empty}FROM {"0x" + Convert.ToHexString(kvp.Value)}\r\n{"WITH PERMISSION_SET = " + PermissionSet}";
				}

				return
					$"ALTER ASSEMBLY [{Name}]\r\nADD FILE FROM {"0x" + Convert.ToHexString(kvp.Value)}\r\nAS N\'{kvp.Key}\'";
			});

			var script = string.Join("\r\nGO\r\n", commands.ToArray());
			return script;
		}

		public string ScriptDrop() {
			return $"DROP ASSEMBLY [{Name}]";
		}
	}
}


//class SoapHexBinary
//{
//	public SoapHexBinary(byte[] bytes)
//	{
//	}

//	public byte Value { get; set; }

//	public static SoapHexBinary Parse(string value) => new SoapHexBinary(new byte[] { });
//}
