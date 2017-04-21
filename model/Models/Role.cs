using System.Collections.Generic;
using System.Text;
namespace SchemaZen.Library.Models {

	public class Role : IScriptable, INameable {
		public string Name { get; set; }
		private List<string> Permissions;

		public Role(string name) {
			Name = name;
			Permissions = new List<string>();
		}

		public void AddPermission(string perm) {
			Permissions.Add(perm);
		}

		public string ScriptCreate() {
			var text = new StringBuilder();

			//First create the Role
			text.AppendFormat("CREATE ROLE [{0}]", Name);
			text.AppendLine();
			text.AppendLine("GO");
			text.AppendLine();

			//now we add all the permissions
			foreach (var p in Permissions) {
				text.AppendLine(p);
			}

			return text.ToString();
		}
	}
}
