using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace SchemaZen.Library.Models {
	public class SqlUser : INameable, IHasOwner, IScriptable {
		public List<string> DatabaseRoles = new List<string>();
		public string Owner { get; set; }
		public string Name { get; set; }
		public byte[] PasswordHash { get; set; }

		public SqlUser(string name, string owner) {
			Name = name;
			Owner = owner;
		}

		public string ScriptDrop() {
			return $"DROP USER [{Name}]";
			// NOTE: login is deliberately not dropped
		}

		public string ScriptCreate() {
			var login = PasswordHash == null ? string.Empty : $@"IF SUSER_ID('{Name}') IS NULL
				BEGIN CREATE LOGIN {Name} WITH PASSWORD = {"0x" + new SoapHexBinary(PasswordHash)} HASHED END
";

			return login + $"CREATE USER [{Name}] {(PasswordHash == null ? "WITHOUT LOGIN" : "FOR LOGIN " + Name)} {(string.IsNullOrEmpty(Owner) ? string.Empty : "WITH DEFAULT_SCHEMA = ")}{Owner}"
				   + "\r\n" +
				   string.Join("\r\n",
					   DatabaseRoles.Select(
						   r => $"/*ALTER ROLE {r} ADD MEMBER {Name}*/ exec sp_addrolemember '{r}', '{Name}'")
						   .ToArray());
		}
	}
}
