using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace model {
    public class Constraint {
        [XmlArrayItem("Column")]
        public List<string> Columns = new List<string>();
        [XmlArrayItem("Column")]
        public List<string> IncludedColumns = new List<string>();

        [XmlAttribute]
        public string Name;
        [XmlAttribute]
        [DefaultValue(false)]
        public bool Clustered;
        public TableInfo Table { get; set; }
        [XmlAttribute]
        public string Type;
        [XmlAttribute]
        [DefaultValue(false)]
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

        public bool HasSameProperties(Constraint other, CompareConfig compareConfig) {
            return IsSimilar(other)
                   && (Name == other.Name || compareConfig.IgnoreConstraintsNameMismatch)
                   && Clustered == other.Clustered
                   && Unique == other.Unique;
        }

        public bool IsSimilar(Constraint other) {
            return HasSameColumns(other)
                   && HasSameIncludedColumns(other)
                   && Table.Name == other.Table.Name
                   && Table.Owner == other.Table.Owner
                   && Type == other.Type;
        }

        public string Script() {
            if (Type == "INDEX") {
                string sql = string.Format("CREATE {0} {1} INDEX [{2}] ON [{3}].[{4}] ([{5}])",
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

        public string ScriptCreate() {
            if (Type == "INDEX") {
                return Script();
            }

            return string.Format("ALTER TABLE [{0}].[{1}] ADD {2}\r\n", Table.Owner, Table.Name, Script());
        }

        public string ScriptDrop() {
            if (Type == "INDEX") {
                return string.Format("DROP INDEX [{0}]\r\n", Name);
            }

            return string.Format("ALTER TABLE [{0}].[{1}] DROP CONSTRAINT [{2}]\r\n", Table.Owner, Table.Name, Name);
        }

        private bool HasSameColumns(Constraint other) {
            return this.Columns.Join(other.Columns, x => x, y => y, (x, y) => x == y).All(equal => equal);
        }

        private bool HasSameIncludedColumns(Constraint other) {
            return this.IncludedColumns.Join(other.IncludedColumns, x => x, y => y, (x, y) => x == y).All(equal => equal);
        }
    }
}