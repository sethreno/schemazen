﻿using System.Collections.Generic;
using System.Linq;
using SchemaZen.Library.Extensions;

namespace SchemaZen.Library.Models
{
	public class Constraint : INameable, IScriptable
	{
		public string IndexType { get; set; }
		public List<ConstraintColumn> Columns { get; set; } = new List<ConstraintColumn>();
		public List<string> IncludedColumns { get; set; } = new List<string>();
		public string Name { get; set; }
		public Table Table { get; set; }
		public string Type { get; set; }
		public string Filter { get; set; }
		public bool Unique { get; set; }
		private bool _isNotForReplication;
		private bool _isSystemNamed;
		public string CheckConstraintExpression { get; private set; }

		public bool ScriptInline { get { return Table == null || (Table != null && Table.IsType); } }

		public Constraint(string name, string type, string columns)
		{
			Name = name;
			Type = type;
			if (!string.IsNullOrEmpty(columns))
			{
				Columns = new List<ConstraintColumn>(columns.Split(',')
					.Select(x => new ConstraintColumn(x, false)));
			}
		}

		public static Constraint CreateCheckedConstraint(string name, bool isNotForReplication, bool isSystemNamed,
			string checkConstraintExpression)
		{
			var constraint = new Constraint(name, "CHECK", "")
			{
				_isNotForReplication = isNotForReplication,
				_isSystemNamed = isSystemNamed,
				CheckConstraintExpression = checkConstraintExpression
			};
			return constraint;
		}

		public string UniqueText => Type != " PRIMARY KEY" && !Unique ? "" : " UNIQUE";

		public string ScriptCreate()
		{
			switch (Type)
			{
				case "CHECK":
					var notForReplicationOption = _isNotForReplication ? "NOT FOR REPLICATION" : "";
					if (ScriptInline)
					{
						return $"CHECK {notForReplicationOption} {CheckConstraintExpression}";
					}
					else
					{
						return $"ALTER TABLE [{Table.Owner}].[{Table.Name}] WITH CHECK ADD {(_isSystemNamed ? string.Empty : $"CONSTRAINT [{Name}]")} CHECK {notForReplicationOption} {CheckConstraintExpression}";
					}
				case "INDEX" when IndexType.Equals("CLUSTERED COLUMNSTORE"):
					{
						var sql = $"CREATE{UniqueText}{IndexType.Space()} INDEX [{Name}] ON [{Table.Owner}].[{Table.Name}]";
						if (!string.IsNullOrEmpty(Filter))
						{
							sql += $" WHERE {Filter}";
						}
						return sql;
					}
				case "INDEX" when IndexType.Equals("NONCLUSTERED COLUMNSTORE"):
					{
						var sql = $"CREATE{UniqueText}{IndexType.Space()} INDEX [{Name}] ON [{Table.Owner}].[{Table.Name}] ({string.Join(", ", Columns.Select(c => c.Script()).ToArray())})";
						if (!string.IsNullOrEmpty(Filter))
						{
							sql += $" WHERE {Filter}";
						}
						return sql;
					}
				case "INDEX":
					{
						var sql =
						$"CREATE{UniqueText}{IndexType.Space()} INDEX [{Name}] ON [{Table.Owner}].[{Table.Name}] ({string.Join(", ", Columns.Select(c => c.Script()).ToArray())})";
						if (IncludedColumns.Count > 0)
						{
							sql += $" INCLUDE ([{string.Join("], [", IncludedColumns.ToArray())}])";
						}



						if (!string.IsNullOrEmpty(Filter))
						{
							sql += $" WHERE {Filter}";
						}



						return sql;
					}
			}

			return (Table.IsType || _isSystemNamed ? string.Empty : $"CONSTRAINT [{Name}] ") +
				$"{Type}{IndexType.Space()} ({string.Join(", ", Columns.Select(c => c.Script()).ToArray())})";
		}
	}
}
