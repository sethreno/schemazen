using System;
using System.Linq;

namespace model
{
	public class Routine
	{
		public bool AnsiNull;
		public string Name;
		public bool QuotedId;
		public string Schema;
		public string Text;
		public RoutineKind RoutineType;

		public enum RoutineKind
		{
			Procedure,
			Function,
			Trigger,
			View,
			XmlSchemaCollection
		}

		public Routine(string schema, string name)
		{
			this.Schema = schema;
			this.Name = name;
		}

		public string ScriptCreate(Database db)
		{
			var script = "";
			var defaultQuotedId = !this.QuotedId;
			if (db != null && db.FindProp("QUOTED_IDENTIFIER") != null)
			{
				defaultQuotedId = db.FindProp("QUOTED_IDENTIFIER").Value == "ON";
			}
			if (defaultQuotedId != this.QuotedId)
			{
				script = string.Format(@"SET QUOTED_IDENTIFIER {0} {1}GO{1}",
					(this.QuotedId ? "ON" : "OFF"), Environment.NewLine);
			}
			var defaultAnsiNulls = !this.AnsiNull;
			if (db != null && db.FindProp("ANSI_NULLS") != null)
			{
				defaultAnsiNulls = db.FindProp("ANSI_NULLS").Value == "ON";
			}
			if (defaultAnsiNulls != this.AnsiNull)
			{
				script = string.Format(@"SET ANSI_NULLS {0} {1}GO{1}",
					(this.AnsiNull ? "ON" : "OFF"), Environment.NewLine);
			}
			return script + this.Text;
		}

		public string GetSQLType()
		{
			var text = this.RoutineType.ToString();
			return string.Join(string.Empty, text.AsEnumerable().Select(
				(c, i) => ((char.IsUpper(c) || i == 0) ? " " + char.ToUpper(c).ToString() : c.ToString())
			).ToArray()).Trim();
		}

		public string ScriptDrop()
		{
			return string.Format("DROP {0} [{1}].[{2}]", this.GetSQLType(), this.Schema, this.Name);
		}
	}
}