namespace SchemaZen.Library.Models {
	public class ConstraintColumn {
		public string ColumnName { get; }
		public bool Desc { get; }

		public ConstraintColumn(string columnName, bool desc) {
			ColumnName = columnName;
			Desc = desc;
		}

		public string Script() {
			return "[" + ColumnName + "]" + (Desc ? " DESC" : "");
		}
	}
}
