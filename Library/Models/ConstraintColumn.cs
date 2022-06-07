namespace SchemaZen.Library.Models;

public class ConstraintColumn {
	public ConstraintColumn(string columnName, bool desc) {
		ColumnName = columnName;
		Desc = desc;
	}

	public string ColumnName { get; }
	public bool Desc { get; }

	public string Script() {
		return "[" + ColumnName + "]" + (Desc ? " DESC" : "");
	}
}
