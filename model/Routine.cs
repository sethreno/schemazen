namespace model {
	public class Routine {
		public bool AnsiNull;
		public string Name;
		public bool QuotedId;
		public string Schema;
		public string Text;
		public string Type;

		public Routine(string schema, string name) {
			Schema = schema;
			Name = name;
		}

		public string ScriptCreate() {
			return string.Format(@"SET QUOTED_IDENTIFIER {0}
GO
SET ANSI_NULLS {1}
GO
{2}", (QuotedId ? "ON" : "OFF"), (AnsiNull ? "ON" : "OFF"), Text);
		}

		public string ScriptDrop() {
			return string.Format("DROP {0} [{1}].[{2}]", Type, Schema, Name);
		}
	}
}