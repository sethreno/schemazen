using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace model {
	public class ColumnList : List<Column> {
		public Column Find(string name) {
		    return this.FirstOrDefault(c => c.Name == name);
		}

	    public string Script() {
			var text = new StringBuilder();
			foreach (Column c in this) {
				text.Append("   " + c.Script());
				if (this.IndexOf(c) < this.Count - 1) {
					text.AppendLine(",");
				}
				else {
					text.AppendLine();
				}
			}
			return text.ToString();
		}
	}
}