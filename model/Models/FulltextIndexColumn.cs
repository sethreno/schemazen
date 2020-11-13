using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SchemaZen.Library.Models {
	public class FulltextIndexColumn {
		public string ColumnName { get; set; }
		public bool StatisticalSemantics { get; set; }
		public int LanguageId { get; set; }

		public string ScriptCreate() {
			return $"    [{ColumnName}] LANGUAGE {LanguageId}{(StatisticalSemantics ? " STATISTICAL_SEMANTICS" : string.Empty)}";
		}
	}
}
