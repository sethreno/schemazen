using System.Collections.Generic;
using System.Linq;

namespace SchemaZen.Library.Models {
	public class Constraint : INameable, IScriptable {
		public bool Clustered { get; set; }
		public List<ConstraintColumn> Columns { get; set; } = new List<ConstraintColumn>();
		public List<string> IncludedColumns { get; set; } = new List<string>();
		public string Name { get; set; }
		public Table Table { get; set; }
		public string Type { get; set; }
		public string Filter { get; set; }
		public bool Unique { get; set; }
		private bool _isNotForReplication;
		private string _checkConstraintExpression;

		public Constraint(string name, string type, string columns) {
			Name = name;
			Type = type;
			if (!string.IsNullOrEmpty(columns)) {
				Columns = new List<ConstraintColumn>(columns.Split(',').Select(x => new ConstraintColumn(x, false)));
			}
		}

		public static Constraint CreateCheckedConstraint(string name, bool isNotForReplication, string checkConstraintExpression) {
			var constraint = new Constraint(name, "CHECK", "") {
				_isNotForReplication = isNotForReplication,
				_checkConstraintExpression = checkConstraintExpression
			};
			return constraint;
		}

		public string ClusteredText => !Clustered ? "NONCLUSTERED" : "CLUSTERED";

		public string UniqueText => Type != "PRIMARY KEY" && !Unique ? "" : "UNIQUE";

		public string ScriptCreate() {
			switch (Type) {
				case "CHECK":
					var notForReplicationOption = _isNotForReplication ? "NOT FOR REPLICATION" : "";
					return $"CONSTRAINT [{Name}] CHECK {notForReplicationOption} {_checkConstraintExpression}";
				case "INDEX":
					var sql = $"CREATE {UniqueText} {ClusteredText} INDEX [{Name}] ON [{Table.Owner}].[{Table.Name}] ({string.Join(", ", Columns.Select(c => c.Script()).ToArray())})";
					if (IncludedColumns.Count > 0) {
						sql += $" INCLUDE ([{string.Join("], [", IncludedColumns.ToArray())}])";
					}
					if (!string.IsNullOrEmpty(Filter)) {
						sql += $" WHERE {Filter}";
					}
					return sql;
			}

			return (Table.IsType ? string.Empty : $"CONSTRAINT [{Name}] ") + $"{Type} {ClusteredText} ({string.Join(", ", Columns.Select(c => c.Script()).ToArray())})";
		}
	}
}
