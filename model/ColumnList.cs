using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace SchemaZen.model {
	public class ColumnList
	{
		private readonly List<Column> _mItems = new List<Column>();

		public ReadOnlyCollection<Column> Items
		{
			get { return _mItems.AsReadOnly(); }
		}

		public void Add(Column c)
		{
			_mItems.Add(c);
		}

		public void Remove(Column c)
		{
			_mItems.Remove(c);
		}

		public Column Find(string name)
		{
			return _mItems.FirstOrDefault(c => c.Name == name);
		}

		public string Script()
		{
			var text = new StringBuilder();
			var index = 0;
			foreach (var c in _mItems)
			{
				text.Append("   ");
				if (index++ > 0)
				{
					text.Append(",");
				}
				else {
					text.Append(" ");
				}
				text.AppendLine(c.ScriptCreate());
			}
			return text.ToString();
		}
	}
}
