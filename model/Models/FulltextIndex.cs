using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SchemaZen.Library.Models {
	public class FulltextIndex : IScriptable, INameable {
		public string SchemaName { get; set; }
		public string TableName { get; set; }
		public string CatalogName { get; set; }
		public List<FulltextIndexColumn> Columns { get; private set; } = new List<FulltextIndexColumn>();
		public string KeyIndex { get; set; }
		public string Name { get { return $"{SchemaName}.{TableName}"; } set { } }

		public string ScriptCreate() {
			var text = new StringBuilder();
			text.AppendLine($"CREATE FULLTEXT INDEX ON [{SchemaName}].[{TableName}] (");

			for (int i = 0; i < Columns.Count; i++) {
				text.AppendLine($"{Columns[i].ScriptCreate()}{(i < Columns.Count - 1 ? "," : string.Empty)}");
			}

			text.AppendLine(")");
			text.AppendLine($"KEY INDEX {KeyIndex}");
			text.AppendLine($"ON {CatalogName}");

			return text.ToString();
		}

		public string ScriptDrop() {
			var text = new StringBuilder();
			text.AppendLine($"DROP FULLTEXT INDEX ON [{SchemaName}].[{TableName}]");
			return text.ToString();
		}
	}
}
