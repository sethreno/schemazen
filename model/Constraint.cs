using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace model {
	public class Constraint {
		public Table Table;
		public string Name;
		public string Type;
        public bool Unique;
		public bool Clustered;
		public List<string> Columns = new List<string>();
        public List<string> IncludedColumns = new List<string>();

		public string ClusteredText {
			get {
				if (!Clustered) return "NONCLUSTERED";
				return "CLUSTERED";
			}
		}

        public string UniqueText {
            get {
                if (!Unique) return "";
                return "UNIQUE";
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
                var sql = string.Format("CREATE {0} {1} INDEX [{2}] ON [{3}].[{4}] ([{5}])", 
                    UniqueText, ClusteredText, Name, Table.Owner, Table.Name, 
                    string.Join("], [", Columns.ToArray()));
                if (IncludedColumns.Count > 0) {
                    sql += string.Format(" INCLUDE ([{0}])", string.Join("], [", IncludedColumns.ToArray()));
                }
                return sql;
			}
			return string.Format("CONSTRAINT [{0}] {1} {2} ([{3}])", 
                Name, Type, ClusteredText, string.Join("], [", Columns.ToArray()));
		}
	}
}
