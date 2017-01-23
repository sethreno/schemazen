using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SchemaZen.Library.Models
{
	public class FullTextIndex : INameable, IScriptable
	{
		public string Name { get; set; }
		public Dictionary<string, string> Columns = new Dictionary<string, string>();
		public string Catalog { get; set; }
		public string SchemaName { get; set; }
		public bool Enabled { get; set; }
		public Table Table { get; set; }

		public string ScriptCreate() {
			StringBuilder sb = new StringBuilder(300);
			sb.Append(
				$@"
					CREATE FULLTEXT INDEX ON [{SchemaName}].[{Table.Name}] KEY INDEX [{Name}] ON [{Catalog}]
				");
			sb.Append("GO");
			foreach (string key in Columns.Keys) {
				sb.Append(
				$@"
					ALTER FULLTEXT INDEX ON [{SchemaName}].[{Table.Name}] ADD ({key} LANGUAGE {Columns[key]})
				");
				sb.Append("GO");
			}
			sb.Append(
				$@"
					ALTER FULLTEXT INDEX ON [{SchemaName}].[{Table.Name}] {(Enabled ? "ENABLE" : "DISABLE" )}
				");
			sb.Append("GO");
			return sb.ToString();
		}
	}
}
