using System.Collections.Generic;
using System.Text;

namespace model {
	public class ColumnList {
		private readonly List<Column> mItems = new List<Column>();

		public List<Column> Items {
			get { return mItems; }
		}

		public void Add(Column c) {
			mItems.Add(c);
		}

		public void Remove(Column c) {
			mItems.Remove(c);
		}

		public Column Find(string name) {
			foreach (Column c in mItems) {
				if (c.Name == name) return c;
			}
			return null;
		}

		public string Script() {
			var text = new StringBuilder();
			foreach (Column c in mItems) {
				text.Append("   " + c.Script());
				if (mItems.IndexOf(c) < mItems.Count - 1) {
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