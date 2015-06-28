using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace model
{
	public class Synonym
	{
		public string Name;
		public string Schema;
		public string BaseObjectName;

		public Synonym(string name, string schema) {
			Name = name;
			Schema = schema;
		}

		public string ScriptCreate() {
			return string.Format("CREATE SYNONYM [{0}].[{1}] FOR {2}", Schema, Name, BaseObjectName);
		}

		public string ScriptDrop()
		{
			return string.Format("DROP SYNONYM [{0}].[{1}]", Schema, Name);
		}
	}
}
