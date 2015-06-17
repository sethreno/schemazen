using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace model
{
	public class ColumnList
	{
		private readonly List<Column> mItems = new List<Column>();

		public ReadOnlyCollection<Column> Items
		{
			get { return this.mItems.AsReadOnly(); }
		}

		public void Add(Column c)
		{
			this.mItems.Add(c);
		}

		public void Remove(Column c)
		{
			this.mItems.Remove(c);
		}

		public Column Find(string name)
		{
			return this.mItems.FirstOrDefault(c => c.Name == name);
		}

		public string Script()
		{
			var text = new StringBuilder();
			foreach (var c in this.mItems)
			{
				text.Append("   " + c.Script());
				if (this.mItems.IndexOf(c) < this.mItems.Count - 1)
				{
					text.AppendLine(",");
				}
				else
				{
					text.AppendLine();
				}
			}
			return text.ToString();
		}
	}
}