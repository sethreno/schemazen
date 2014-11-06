﻿using System;
using System.Xml.Serialization;

namespace model {
	public class Routine {
		[XmlAttribute]
		public string Name;
		[XmlAttribute]
		public bool AnsiNull;
		[XmlAttribute]
		public bool QuotedId;
		[XmlAttribute]
		public string Schema;
		[XmlAttribute]
		public string Type;
		[XmlText]
		public string Text;

		private Routine() { }

		public Routine(string schema, string name) {
			Schema = schema;
			Name = name;
		}

		public string ScriptCreate(Database db) {
			var script = "";
			var defaultQuotedId = !QuotedId;
			if (db != null && db.FindProp("QUOTED_IDENTIFIER") != null) {
				defaultQuotedId = db.FindProp("QUOTED_IDENTIFIER").Value == "ON";
			}
			if (defaultQuotedId != QuotedId) {
				script = string.Format(@"SET QUOTED_IDENTIFIER {0} {1}GO{1}",
					(QuotedId ? "ON" : "OFF"), Environment.NewLine);
			}
			var defaultAnsiNulls = !AnsiNull;
			if (db != null && db.FindProp("ANSI_NULLS") != null) {
				defaultAnsiNulls = db.FindProp("ANSI_NULLS").Value == "ON";
			}
			if (defaultAnsiNulls != AnsiNull) {
				script = string.Format(@"SET ANSI_NULLS {0} {1}GO{1}",
					(AnsiNull ? "ON" : "OFF"), Environment.NewLine);
			}
			return script + Text;
		}

		public string ScriptDrop() {
			return string.Format("DROP {0} [{1}].[{2}]", Type, Schema, Name);
		}
	}
}