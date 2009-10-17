using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace model {
	public class Constraint {
		public Table Table;
		public string Name;
		public string Type;
		public bool Clustered;
		public List<string> Columns = new List<string>();

		public string ClusteredText {
			get {
				if (!Clustered) return "";
				return "CLUSTERED";
			}
		}

		public Constraint(string name, string type, string columns) {
			this.Name = name;
			this.Type = type;
			if (!string.IsNullOrEmpty(columns)) {
				this.Columns = new List<string>(columns.Split(','));
			}
		}

		public string Script() {
			if (Type == "INDEX") {
				return string.Format("CREATE {0} INDEX [{1}] ON [{2}].[{3}] ([{4}])", ClusteredText, Name, Table.Owner, Table.Name, string.Join("], [", Columns.ToArray()));
			}
			return string.Format("CONSTRAINT [{0}] {1} {2} ([{3}])", Name, Type, ClusteredText, string.Join("], [", Columns.ToArray()));
		}
	}
}
