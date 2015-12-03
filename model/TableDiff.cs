using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SchemaZen.model
{
	public class TableDiff
	{
		public List<Column> ColumnsAdded = new List<Column>();
		public List<ColumnDiff> ColumnsDiff = new List<ColumnDiff>();
		public List<Column> ColumnsDropped = new List<Column>();

		public List<Constraint> ConstraintsAdded = new List<Constraint>();
		public List<Constraint> ConstraintsChanged = new List<Constraint>();
		public List<Constraint> ConstraintsDeleted = new List<Constraint>();

		public readonly string Name;
		public readonly string Owner;

		public TableDiff(Table t1, Table t2)
		{
			if (t1.Owner != t2.Owner || t1.Name != t2.Name)
				throw new ArgumentException("Tables have different names.");
			if (t1.IsType != t2.IsType)
				throw new ArgumentException("Tables must either both be types or tables.");

			Name = t1.Name;
			Owner = t2.Owner;

			//get additions and compare mutual columns
			foreach (var c in t1.Columns.Items)
			{
				var c2 = t2.Columns.Find(c.Name);
				if (c2 == null)
				{
					ColumnsAdded.Add(c);
				}
				else {
					//compare mutual columns
					var cDiff = c.Compare(c2);
					if (cDiff.IsDiff)
					{
						ColumnsDiff.Add(cDiff);
					}
				}
			}

			//get deletions
			foreach (var c in t2.Columns.Items.Where(c => t1.Columns.Find(c.Name) == null))
			{
				ColumnsDropped.Add(c);
			}

			//get added and compare mutual constraints
			foreach (var c in t1.Constraints)
			{
				var c2 = t2.FindConstraint(c.Name);
				if (c2 == null)
				{
					ConstraintsAdded.Add(c);
				}
				else {
					if (c.ScriptCreate() != c2.ScriptCreate())
					{
						ConstraintsChanged.Add(c);
					}
				}
			}
			//get deleted constraints
			foreach (var c in t2.Constraints.Where(c => t1.FindConstraint(c.Name) == null))
			{
				ConstraintsDeleted.Add(c);
			}
		}

		public bool IsDiff
		{
			get
			{
				return ColumnsAdded.Any() || ColumnsDropped.Any() || ColumnsDiff.Any() || ConstraintsAdded.Any() ||
					   ConstraintsChanged.Any() || ConstraintsDeleted.Any();
			}
		}

		public string Script()
		{
			var text = new StringBuilder();

			foreach (var c in ColumnsAdded)
			{
				text.AppendFormat("ALTER TABLE [{0}].[{1}] ADD {2}", Owner, Name, c.ScriptCreate());
				text.AppendLine();
			}

			foreach (var c in ColumnsDropped)
			{
				text.AppendFormat("ALTER TABLE [{0}].[{1}] DROP COLUMN [{2}]", Owner, Name, c.Name);
				text.AppendLine();
			}

			foreach (var c in ColumnsDiff)
			{
				if (c.DefaultIsDiff)
				{
					if (c.Source.Default != null)
					{
						text.AppendFormat("ALTER TABLE [{0}].[{1}] {2}", Owner, Name, c.Source.Default.ScriptDrop());
						text.AppendLine();
					}
					if (c.Target.Default != null)
					{
						text.AppendFormat("ALTER TABLE [{0}].[{1}] {2}", Owner, Name, c.Target.Default.ScriptCreate(c.Target));
						text.AppendLine();
					}
				}
				if (!c.OnlyDefaultIsDiff)
				{
					text.AppendFormat("ALTER TABLE [{0}].[{1}] ALTER COLUMN {2}", Owner, Name, c.Target.ScriptAlter());
					text.AppendLine();
				}
			}
			return text.ToString();
		}
	}
}
