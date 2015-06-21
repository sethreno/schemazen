using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace model {
	public class ColumnList {
		private readonly List<Column> mItems = new List<Column>();

		public ReadOnlyCollection<Column> Items {
			get { return mItems.AsReadOnly(); }
		}

		public void Add(Column c) {
			mItems.Add(c);
		}

		public void Remove(Column c) {
			mItems.Remove(c);
		}

		public Column Find(string name) {
			return mItems.FirstOrDefault(c => c.Name == name);
		}

		public string Script() {
			var text = new StringBuilder();
			foreach (Column c in mItems) {
				text.Append("   " + c.Script());
				if (mItems.IndexOf(c) < mItems.Count - 1) {
					text.AppendLine(",");
				} else {
					text.AppendLine();
				}
			}
			return text.ToString();
		}
	}
}
