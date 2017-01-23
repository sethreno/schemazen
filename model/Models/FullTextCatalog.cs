using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SchemaZen.Library.Models
{
	public class FullTextCatalog : INameable, IScriptable
	{
		public string Name { get; set; }
		public string SchemaName { get; set; }
		public string FileGroup { get; set; }
		public bool AccentSensitivityOn { get; set; }


		public string ScriptCreate() {
			return $@"CREATE FULLTEXT CATALOG [{Name}]
					ON FILEGROUP[{FileGroup}]
					WITH ACCENT_SENSITIVITY = {(AccentSensitivityOn ? "ON" : "OFF")}
					AUTHORIZATION[{SchemaName}]";
		}
	}
}
