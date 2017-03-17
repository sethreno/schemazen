using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SchemaZen.Library.Models {
	public class Schema : INameable, IHasOwner, IScriptable {
		public string Name { get; set; }
		public string Owner { get; set; }

		public Schema(string name, string owner) {
			Owner = owner;
			Name = name;
		}

		public string ScriptCreate() {
			return $@"
if not exists(select s.schema_id from sys.schemas s where s.name = '{Name}') 
	and exists(select p.principal_id from sys.database_principals p where p.name = '{Owner}') begin
	exec sp_executesql N'create schema [{Name}] authorization [{Owner}]'
end
";
		}
	}

	public class Table : INameable, IHasOwner, IScriptable {
	    public ColumnList Columns = new ColumnList();
		private readonly List<Constraint> _constraints = new List<Constraint>();
		public string Name { get; set; }
		public string Owner { get; set; }
		public bool IsType;

	    public Table(string owner, string name) {
			Owner = owner;
			Name = name;
	    }

		public Constraint PrimaryKey {
			get { return _constraints.FirstOrDefault(c => c.Type == "PRIMARY KEY"); }
		}

		public Constraint FindConstraint(string name) {
			return _constraints.FirstOrDefault(c => c.Name == name);
		}

		public IEnumerable<Constraint> Constraints => _constraints.AsEnumerable();
  
	    public void AddConstraint(Constraint constraint) {
			constraint.Table = this;
			_constraints.Add(constraint);
		}

		public void RemoveContraint(Constraint constraint) {
			_constraints.Remove(constraint);
		}

		public TableDiff Compare(Table t) {
			var diff = new TableDiff {
				Owner = t.Owner,
				Name = t.Name
			};

			//get additions and compare mutual columns
			foreach (var c in Columns.Items) {
				var c2 = t.Columns.Find(c.Name);
				if (c2 == null) {
					diff.ColumnsAdded.Add(c);
				} else {
					//compare mutual columns
					var cDiff = c.Compare(c2);
					if (cDiff.IsDiff) {
						diff.ColumnsDiff.Add(cDiff);
					}
				}
			}

			//get deletions
			foreach (var c in t.Columns.Items.Where(c => Columns.Find(c.Name) == null)) {
				diff.ColumnsDropped.Add(c);
			}

			if (!t.IsType) {
				//get added and compare mutual constraints
				foreach (var c in Constraints) {
					var c2 = t.FindConstraint(c.Name);
					if (c2 == null) {
						diff.ConstraintsAdded.Add(c);
					} else {
						if (c.ScriptCreate() != c2.ScriptCreate()) {
							diff.ConstraintsChanged.Add(c);
						}
					}
				}
				//get deleted constraints
				foreach (var c in t.Constraints.Where(c => FindConstraint(c.Name) == null)) {
					diff.ConstraintsDeleted.Add(c);
				}
			} else {
				// compare constraints on table types, which can't be named in the script, but have names in the DB
				var dest = Constraints.ToList();
				var src = t.Constraints.ToList();

				var j = from c1 in dest
						join c2 in src on c1.ScriptCreate() equals c2.ScriptCreate() into match //new { c1.Type, c1.Unique, c1.Clustered, Columns = string.Join(",", c1.Columns.ToArray()), IncludedColumns = string.Join(",", c1.IncludedColumns.ToArray()) } equals new { c2.Type, c2.Unique, c2.Clustered, Columns = string.Join(",", c2.Columns.ToArray()), IncludedColumns = string.Join(",", c2.IncludedColumns.ToArray()) } into match
						from m in match.DefaultIfEmpty()
						select new { c1, m };

				foreach (var c in j) {
					if (c.m == null) {
						diff.ConstraintsAdded.Add(c.c1);
					} else {
						src.Remove(c.m);
					}
				}
				foreach (var c in src) {
					diff.ConstraintsDeleted.Add(c);
				}
			}

			return diff;
		}

		public string ScriptCreate() {
			var text = new StringBuilder();
			text.Append($"CREATE {(IsType ? "TYPE" : "TABLE")} [{Owner}].[{Name}] {(IsType ? "AS TABLE " : string.Empty)}(\r\n");
			text.Append(Columns.Script());
			if (_constraints.Count > 0) text.AppendLine();
			foreach (var c in _constraints.OrderBy(x => x.Name).Where(c => c.Type != "INDEX")) {
				text.AppendLine("   ," + c.ScriptCreate());
			}
			text.AppendLine(")");
			text.AppendLine();
			foreach (var c in _constraints.Where(c => c.Type == "INDEX")) {
				text.AppendLine(c.ScriptCreate());
			}
			return text.ToString();
		}

		public string ScriptDrop() {
			return $"DROP {(IsType ? "TYPE" : "TABLE")} [{Owner}].[{Name}]";
		}


	}

	public class TableDiff {
		public List<Column> ColumnsAdded = new List<Column>();
		public List<ColumnDiff> ColumnsDiff = new List<ColumnDiff>();
		public List<Column> ColumnsDropped = new List<Column>();

		public List<Constraint> ConstraintsAdded = new List<Constraint>();
		public List<Constraint> ConstraintsChanged = new List<Constraint>();
		public List<Constraint> ConstraintsDeleted = new List<Constraint>();
		public string Name;
		public string Owner;

		public bool IsDiff => ColumnsAdded.Count + ColumnsDropped.Count + ColumnsDiff.Count + ConstraintsAdded.Count +
							  ConstraintsChanged.Count + ConstraintsDeleted.Count > 0;

		public string Script() {
			var text = new StringBuilder();

			foreach (var c in ColumnsAdded) {
				text.Append($"ALTER TABLE [{Owner}].[{Name}] ADD {c.ScriptCreate()}\r\n");
			}

			foreach (var c in ColumnsDropped) {
				text.Append($"ALTER TABLE [{Owner}].[{Name}] DROP COLUMN [{c.Name}]\r\n");
			}

			foreach (var c in ColumnsDiff) {
				if (c.DefaultIsDiff) {
					if (c.Source.Default != null) {
						text.Append($"ALTER TABLE [{Owner}].[{Name}] {c.Source.Default.ScriptDrop()}\r\n");
					}
					if (c.Target.Default != null) {
						text.Append($"ALTER TABLE [{Owner}].[{Name}] {c.Target.Default.ScriptCreate(c.Target)}\r\n");
					}
				}
				if (!c.OnlyDefaultIsDiff) {
					text.Append($"ALTER TABLE [{Owner}].[{Name}] ALTER COLUMN {c.Target.ScriptAlter()}\r\n");
				}
			}

			foreach (var c in ConstraintsAdded.Where(c => c.Type == "CHECK")) {
				text.Append($"ALTER TABLE [{Owner}].[{Name}] ADD {c.ScriptCreate()}\r\n");
			}

			foreach (var c in ConstraintsChanged.Where(c => c.Type == "CHECK")) {
				text.Append($"-- Check constraint {c.Name} changed\r\n");
				text.Append($"ALTER TABLE [{Owner}].[{Name}] DROP CONSTRAINT {c.Name}\r\n");
				text.Append($"ALTER TABLE [{Owner}].[{Name}] ADD {c.ScriptCreate()}\r\n");
			}

			foreach (var c in ConstraintsDeleted.Where(c => c.Type == "CHECK")) {
				text.Append($"ALTER TABLE [{Owner}].[{Name}] DROP CONSTRAINT {c.Name}\r\n");
			}

			return text.ToString();
		}
	}
}
