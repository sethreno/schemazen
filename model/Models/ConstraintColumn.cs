namespace SchemaZen.Library.Models {
	public class ConstraintColumn {
		public string ColumnName { get; private set; }
		public bool Desc { get; private set; }

		public ConstraintColumn(string columnName, bool desc) {
			ColumnName = columnName;
			Desc = desc;
		}

		public string Script() {
			return "[" + ColumnName + "]" + (Desc ? " DESC" : "");
		}
	}
}
