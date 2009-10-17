using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace model {
	public class Routine {
		public string Schema;
		public string Name;
		public string Text;
		public string Type;

		public Routine(string schema, string name) {
			this.Schema = schema;
			this.Name = name;
		}

		public string ScriptCreate() {
			return Text;
		}

		public string ScriptDrop() {
			return string.Format("DROP {0} [{1}].[{2}]", Type, Schema, Name);
		}
	}
}
