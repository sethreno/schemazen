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
        public bool AnsiNull;
        public bool QuotedId;

		public Routine(string schema, string name) {
			this.Schema = schema;
			this.Name = name;
		}

		public string ScriptCreate() {
            return string.Format(@"SET QUOTED_IDENTIFIER {0}
GO
SET ANSI_NULLS {1}
GO
{2}", (QuotedId ? "ON" : "OFF"), (AnsiNull ? "ON" : "OFF"), Text);                
		}

		public string ScriptDrop() {
			return string.Format("DROP {0} [{1}].[{2}]", Type, Schema, Name);
		}
	}
}
