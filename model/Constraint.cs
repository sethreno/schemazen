using System.Collections.Generic;

namespace SchemaZen.model {
	public class Constraint : INameable, IScriptable {
		public bool Clustered;
		public List<string> Columns = new List<string>();
		public List<string> IncludedColumns = new List<string>();
		public string Name { get; set; }
		public Table Table;
		public string Type;
		public bool Unique;
		private bool IsNotForReplication;
		private string CheckConstraintExpression;

		public Constraint(string name, string type, string columns) {
			Name = name;
			Type = type;
			if (!string.IsNullOrEmpty(columns)) {
				Columns = new List<string>(columns.Split(','));
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
				return $"CONSTRAINT [{Name}] CHECK {CheckConstraintExpression}";
			}

			if (Type == "INDEX") {
				var sql = string.Format("CREATE {0} {1} INDEX [{2}] ON [{3}].[{4}] ([{5}])", UniqueText, ClusteredText, Name,
					Table.Owner, Table.Name,
					string.Join("], [", Columns.ToArray()));
				if (IncludedColumns.Count > 0) {
					sql += string.Format(" INCLUDE ([{0}])", string.Join("], [", IncludedColumns.ToArray()));
				}
				return sql;
			}
			return (Table.IsType ? string.Empty : string.Format("CONSTRAINT [{0}] ", Name)) +
				string.Format("{0} {1} ([{2}])", Type, ClusteredText, string.Join("], [", Columns.ToArray()));
		}
	}
}
