using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace SchemaZen.model {
	public class SqlUser : INameable, IHasOwner, IScriptable {
		public List<string> DatabaseRoles = new List<string>();
		public string Owner { get; set; }
		public string Name { get; set; }
		public byte[] PasswordHash;

		public SqlUser(string name, string owner) {
			Name = name;
			Owner = owner;
		}

		public string ScriptDrop() {
			return string.Format("DROP {0} [{1}]", "USER", Name);
			// NOTE: login is deliberately not dropped
		}

		public string ScriptCreate() {
			var login = PasswordHash == null ? string.Empty : string.Format(@"IF SUSER_ID('{0}') IS NULL
				BEGIN CREATE LOGIN {0} WITH PASSWORD = {1} HASHED END
", Name, "0x" + new SoapHexBinary(PasswordHash));

			return login +
			       string.Format("CREATE USER {0} {1} {2}{3}", Name, PasswordHash == null ? "WITHOUT LOGIN" : "FOR LOGIN " + Name,
				       string.IsNullOrEmpty(Owner) ? string.Empty : "WITH DEFAULT_SCHEMA = ", Owner)
			       + "\r\n" +
			       string.Join("\r\n",
				       DatabaseRoles.Select(
					       r => string.Format("/*ALTER ROLE {0} ADD MEMBER {1}*/ exec sp_addrolemember '{0}', '{1}'", r, Name))
					       .ToArray());
		}
	}
}
