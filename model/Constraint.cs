using System.Collections.Generic;

namespace model
{
	public class Constraint
	{
		public bool Clustered;
		public List<string> Columns = new List<string>();
		public List<string> IncludedColumns = new List<string>();
		public string Name;
		public Table Table;
		public string Type;
		public bool Unique;

		public Constraint(string name, string type, string columns)
		{
			this.Name = name;
			this.Type = type;
			if (!string.IsNullOrEmpty(columns))
			{
				this.Columns = new List<string>(columns.Split(','));
			}
		}

		public string ClusteredText
		{
			get
			{
				return !this.Clustered ? "NONCLUSTERED" : "CLUSTERED";
			}
		}

		public string UniqueText
		{
			get
			{
				return !this.Unique ? "" : "UNIQUE";
			}
		}

		public string Script()
		{
			if (this.Type == "INDEX")
			{
				var sql = string.Format("CREATE {0} {1} INDEX [{2}] ON [{3}].[{4}] ([{5}])", this.UniqueText, this.ClusteredText, this.Name, this.Table.Owner, this.Table.Name,
					string.Join("], [", this.Columns.ToArray()));
				if (this.IncludedColumns.Count > 0)
				{
					sql += string.Format(" INCLUDE ([{0}])", string.Join("], [", this.IncludedColumns.ToArray()));
				}
				return sql;
			}
			return string.Format("CONSTRAINT [{0}] {1} {2} ([{3}])", this.Name, this.Type, this.ClusteredText, string.Join("], [", this.Columns.ToArray()));
		}
	}
}