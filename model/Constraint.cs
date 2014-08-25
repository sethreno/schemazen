using System.Collections.Generic;

namespace model {
    public class Constraint {
        public bool Clustered;
        public List<string> Columns = new List<string>();
        public List<string> IncludedColumns = new List<string>();
        public string Name;
        public string TableName { get; set; }
        public string TableOwner { get; set; }
        public string Type;
        public bool Unique;

        private Constraint() { }

        public Constraint(string name, string type, string columns) {
            Name = name;
            Type = type;
            if (!string.IsNullOrEmpty(columns)) {
                Columns = new List<string>(columns.Split(','));
            }
        }

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



        public string Script() {
            if (Type == "INDEX") {
                string sql = string.Format("CREATE {0} {1} INDEX [{2}] ON [{3}].[{4}] ([{5}])",
                    UniqueText, ClusteredText, Name, TableOwner, TableName,
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