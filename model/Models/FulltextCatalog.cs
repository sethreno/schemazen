using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SchemaZen.Library.Models {
	public class FulltextCatalog : INameable, IScriptable {
		public FulltextCatalog(string name) {
			Name = name;
		}

		public string Name { get; set; }
		public bool IsAccentSensitivityOn { get; set; }
		public bool IsDefault { get; set; }

		public string ScriptCreate() {
			var text = new StringBuilder();
			text.AppendLine(
				$"CREATE FULLTEXT CATALOG [{Name}]{(IsAccentSensitivityOn ? " WITH ACCENT_SENSITIVITY = ON" : string.Empty)}{(IsDefault ? " AS DEFAULT" : string.Empty)}");

			return text.ToString();
		}

		public string ScriptDrop() {
			var text = new StringBuilder();
			text.AppendLine(
				$"DROP FULLTEXT CATALOG [{Name}]");

			return text.ToString();
		}
	}
}
