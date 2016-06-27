using System.Collections.Generic;
using System.Linq;

namespace SchemaZen.Library.Models {
	public class Constraint : INameable, IScriptable {
		public bool Clustered;
		public List<ConstraintColumn> Columns = new List<ConstraintColumn>();
		public List<string> IncludedColumns = new List<string>();
		public string Name { get; set; }
		public Table Table;
		public string Type;
		public string Filter;
		public bool Unique;
		private bool IsNotForReplication;
		private string CheckConstraintExpression;

		public Constraint(string name, string type, string columns) {
			Name = name;
			Type = type;
			if (!string.IsNullOrEmpty(columns)) {
				Columns = new List<ConstraintColumn>(columns.Split(',').Select(x => new ConstraintColumn(x, false)));
			}
		}

		public static Constraint CreateCheckedConstraint(string name, bool isNotForReplication, string checkConstraintExpression) {
			var constraint = new Constraint(name, "CHECK", "") {
				IsNotForReplication = isNotForReplication,
				CheckConstraintExpression = checkConstraintExpression
			};
			return constraint;
		}

		public string ClusteredText {
			get { return !Clustered ? "NONCLUSTERED" : "CLUSTERED"; }
		}

		public string UniqueText {
			get { return Type != "PRIMARY KEY" && !Unique ? "" : "UNIQUE"; }
		}

		public string ScriptCreate() {
			if (Type == "CHECK") {
				var notForReplicationOption = IsNotForReplication ? "NOT FOR REPLICATION" : "";
				return $"CONSTRAINT [{Name}] CHECK {notForReplicationOption} {CheckConstraintExpression}";
			}

			if (Type == "INDEX") {
				var sql = string.Format("CREATE {0} {1} INDEX [{2}] ON [{3}].[{4}] ({5})", UniqueText, ClusteredText, Name,
					Table.Owner, Table.Name,
					string.Join(", ", Columns.Select(c => c.Script()).ToArray()));
				if (IncludedColumns.Count > 0) {
					sql += string.Format(" INCLUDE ([{0}])", string.Join("], [", IncludedColumns.ToArray()));
				}
				if (!string.IsNullOrEmpty(Filter))
				{
				sql += string.Format(" WHERE {0}", Filter);
				}
				return sql;
			}
			return (Table.IsType ? string.Empty : string.Format("CONSTRAINT [{0}] ", Name)) +
				string.Format("{0} {1} ({2})", Type, ClusteredText, string.Join(", ", Columns.Select(c => c.Script()).ToArray()));
		}
	}
}
