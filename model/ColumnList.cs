using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace model {
	using System.Collections.ObjectModel;

	public class ColumnList {

		private List<Column> mItems = new List<Column>();
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
			foreach (Column c in mItems) {
				if (c.Name == name) return c;
			}
			return null;
		}

		public string Script() {
			StringBuilder text = new StringBuilder();
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
