using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SchemaZen.model {
	public class Routine : INameable, IHasOwner {
		public enum RoutineKind {
			Procedure,
			Function,
			Trigger,
			View,
			XmlSchemaCollection
		}

		public bool AnsiNull;
		public string Name { get; set; }
		public bool QuotedId;
		public RoutineKind RoutineType;
		public string Owner { get; set; }
		public string Text;
		public bool Disabled;
		public string RelatedTableSchema;
		public string RelatedTableName;
		public Database Db;

		private const string SqlCreateRegex =
			@"\A" + Database.SqlWhitespaceOrCommentRegex + @"*?(CREATE)" + Database.SqlWhitespaceOrCommentRegex;

		private const string SqlCreateWithNameRegex =
			SqlCreateRegex + @"+{0}" + Database.SqlWhitespaceOrCommentRegex + @"+?(?:(?:(" + Database.SqlEnclosedIdentifierRegex +
			@"|" + Database.SqlRegularIdentifierRegex + @")\.)?(" + Database.SqlEnclosedIdentifierRegex + @"|" +
			Database.SqlRegularIdentifierRegex + @"))(?:\(|" + Database.SqlWhitespaceOrCommentRegex + @")";

		public Routine(string owner, string name, Database db) {
			Owner = owner;
			Name = name;
			Db = db;
		}

		private string ScriptQuotedIdAndAnsiNulls(Database db, bool databaseDefaults) {
			var script = "";
			var defaultQuotedId = !QuotedId;
			if (db != null && db.FindProp("QUOTED_IDENTIFIER") != null) {
				defaultQuotedId = db.FindProp("QUOTED_IDENTIFIER").Value == "ON";
			}
			if (defaultQuotedId != QuotedId) {
				script += string.Format(@"SET QUOTED_IDENTIFIER {0} {1}GO{1}",
					((databaseDefaults ? defaultQuotedId : QuotedId) ? "ON" : "OFF"), Environment.NewLine);
			}
			var defaultAnsiNulls = !AnsiNull;
			if (db != null && db.FindProp("ANSI_NULLS") != null) {
				defaultAnsiNulls = db.FindProp("ANSI_NULLS").Value == "ON";
			}
			if (defaultAnsiNulls != AnsiNull) {
				script += string.Format(@"SET ANSI_NULLS {0} {1}GO{1}",
					((databaseDefaults ? defaultAnsiNulls : AnsiNull) ? "ON" : "OFF"), Environment.NewLine);
			}
			return script;
		}

		private string ScriptBase(string definition) {
			var before = ScriptQuotedIdAndAnsiNulls(Db, false);
			var after = ScriptQuotedIdAndAnsiNulls(Db, true);
			if (!string.IsNullOrEmpty(after))
				after = Environment.NewLine + "GO" + Environment.NewLine + after;

			if (RoutineType == RoutineKind.Trigger)
				after +=
					string.Format("{0} TRIGGER [{1}].[{2}] ON [{3}].[{4}]", Disabled ? "DISABLE" : "ENABLE", Owner, Name,
						RelatedTableSchema, RelatedTableName) + Environment.NewLine + "GO" + Environment.NewLine;

			if (string.IsNullOrEmpty(definition))
				definition = string.Format("/* missing definition for {0} [{1}].[{2}] */", RoutineType, Owner, Name);

			return before + definition + after;
		}

		public string ScriptCreate() {
			return ScriptBase(Text);
		}

		public string GetSQLTypeForRegEx() {
			var text = GetSQLType();
			if (RoutineType == RoutineKind.Procedure) // support shorthand - PROC
				return "(?:" + text + "|" + text.Substring(0, 4) + ")";
			return text;
		}

		public string GetSQLType() {
			var text = RoutineType.ToString();
			return string.Join(string.Empty, text.AsEnumerable().Select(
				(c, i) => ((char.IsUpper(c) || i == 0) ? " " + char.ToUpper(c).ToString() : c.ToString())
				).ToArray()).Trim();
		}

		public string ScriptDrop() {
			return string.Format("DROP {0} [{1}].[{2}]", GetSQLType(), Owner, Name);
		}


		public string ScriptAlter() {
			if (RoutineType != RoutineKind.XmlSchemaCollection) {
				var regex = new Regex(SqlCreateRegex, RegexOptions.IgnoreCase);
				var match = regex.Match(Text);
				var group = match.Groups[1];
				if (group.Success) {
					return ScriptBase(Text.Substring(0, group.Index) + "ALTER" + Text.Substring(group.Index + group.Length));
				}
			}
			throw new Exception(string.Format("Unable to script routine {0} {1}.{2} as ALTER", RoutineType, Owner, Name));
		}

		public IEnumerable<string> Warnings() {
			if (string.IsNullOrEmpty(Text)) {
				yield return "Script definition could not be retrieved.";
			} else {
				// check if the name is correct
				var regex = new Regex(string.Format(SqlCreateWithNameRegex, GetSQLTypeForRegEx()),
					RegexOptions.IgnoreCase | RegexOptions.Singleline);
				var match = regex.Match(Text);

				// the schema is captured in group index 2, and the name in 3

				var nameGroup = match.Groups[3];
				if (nameGroup.Success) {
					var name = nameGroup.Value;
					if (name.StartsWith("[") && name.EndsWith("]"))
						name = name.Substring(1, name.Length - 2);

					if (string.Compare(Name, name, StringComparison.InvariantCultureIgnoreCase) != 0) {
						yield return string.Format("Name from script definition '{0}' does not match expected name '{1}'", name, Name);
					}
				}
			}
		}
	}
}
