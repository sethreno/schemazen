using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;

namespace model
{
	public class SqlUser
	{
		public string Name;
		public byte[] PasswordHash;
		public string DefaultSchema;
		public List<string> DatabaseRoles = new List<string>();

		public SqlUser(string name, string defaultSchema)
		{
			this.Name = name;
			this.DefaultSchema = defaultSchema;
		}

		public string ScriptDrop()
		{
			return string.Format("DROP {0} [{1}]", "USER", this.Name);
			// NOTE: login is deliberately not dropped
		}

		public string ScriptCreate(Database db)
		{
			var login = this.PasswordHash == null ? string.Empty : string.Format(@"IF SUSER_ID('{0}') IS NULL
				BEGIN CREATE LOGIN {0} WITH PASSWORD = {1} HASHED END
", this.Name, "0x" + new SoapHexBinary(this.PasswordHash).ToString());

			return login + string.Format("CREATE USER {0} {1} {2}{3}", this.Name, this.PasswordHash == null ? "WITHOUT LOGIN" : "FOR LOGIN " + this.Name, string.IsNullOrEmpty(this.DefaultSchema) ? string.Empty : "WITH DEFAULT_SCHEMA = ", this.DefaultSchema)
			+ "\r\n" + string.Join("\r\n", this.DatabaseRoles.Select(r => string.Format("/*ALTER ROLE {0} ADD MEMBER {1}*/ exec sp_addrolemember '{0}', '{1}'", r, this.Name)).ToArray());

		}
	}
}
