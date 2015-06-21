using System.Collections.Generic;

namespace model {
	public class Constraint {
		public bool Clustered;
		public List<string> Columns = new List<string>();
		public List<string> IncludedColumns = new List<string>();
		public string Name;
		public Table Table;
		public string Type;
		public bool Unique;

		public Constraint(string name, string type, string columns) {
			Name = name;
			Type = type;
			if (!string.IsNullOrEmpty(columns)) {
				Columns = new List<string>(columns.Split(','));
			}
		}

		public string ClusteredText {
			get { return !Clustered ? "NONCLUSTERED" : "CLUSTERED"; }
		}

		public string UniqueText {
			get { return !Unique ? "" : "UNIQUE"; }
		}

		public string Script() {
			if (Type == "INDEX") {
				string sql = string.Format("CREATE {0} {1} INDEX [{2}] ON [{3}].[{4}] ([{5}])", UniqueText, ClusteredText, Name,
					Table.Owner, Table.Name,
					string.Join("], [", Columns.ToArray()));
				if (IncludedColumns.Count > 0) {
					sql += string.Format(" INCLUDE ([{0}])", string.Join("], [", IncludedColumns.ToArray()));
				}
				return sql;
			}
			return string.Format("CONSTRAINT [{0}] {1} {2} ([{3}])", Name, Type, ClusteredText,
				string.Join("], [", Columns.ToArray()));
		}
	}
}
