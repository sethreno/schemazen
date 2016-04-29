using System;

namespace SchemaZen.model {
	public class Column {
		public Default Default;
		public Identity Identity;
		public bool IsNullable;
		public int Length;
		public string Name;
		public int Position;
		public byte Precision;
		public int Scale;
		public string Type;
		public string ComputedDefinition;
		public bool IsRowGuidCol;

		public Column() { }

		public Column(string name, string type, bool nullable, Default defaultValue) {
			Name = name;
			Type = type;
			Default = defaultValue;
			IsNullable = nullable;
		}

		public Column(string name, string type, int length, bool nullable, Default defaultValue)
			: this(name, type, nullable, defaultValue) {
			Length = length;
		}

		public Column(string name, string type, byte precision, int scale, bool nullable, Default defaultValue)
			: this(name, type, nullable, defaultValue) {
			Precision = precision;
			Scale = scale;
		}

		private string IsNullableText {
			get {
				if (IsNullable || !string.IsNullOrEmpty(ComputedDefinition)) return "NULL";
				return "NOT NULL";
			}
		}

		public string DefaultText {
			get {
				if (Default == null || !string.IsNullOrEmpty(ComputedDefinition)) return "";
				return "\r\n      " + Default.ScriptAsPartOfColumnDefinition();
			}
		}

		public string IdentityText {
			get {
				if (Identity == null) return "";
				return "\r\n      " + Identity.Script();
			}
		}

		public string RowGuidColText {
			get { return IsRowGuidCol ? "ROWGUIDCOL" : string.Empty; }
		}

		public ColumnDiff Compare(Column c) {
			return new ColumnDiff(this, c);
		}

		private string ScriptBase(bool includeDefaultConstraint) {
			if (string.IsNullOrEmpty(ComputedDefinition)) {
				switch (Type) {
					case "bigint":
					case "bit":
					case "date":
					case "datetime":
					case "datetime2":
					case "datetimeoffset":
					case "float":
					case "hierarchyid":
					case "image":
					case "int":
					case "money":
					case "ntext":
					case "real":
					case "smalldatetime":
					case "smallint":
					case "smallmoney":
					case "sql_variant":
					case "text":
					case "time":
					case "timestamp":
					case "tinyint":
					case "uniqueidentifier":
					case "geography":
					case "xml":
                    case "sysname":
						return string.Format("[{0}] [{1}] {2} {3} {4} {5}", Name, Type, IsNullableText,
							includeDefaultConstraint ? DefaultText : string.Empty, IdentityText, RowGuidColText);
					case "binary":
					case "char":
					case "nchar":
					case "nvarchar":
					case "varbinary":
					case "varchar":
						var lengthString = Length.ToString();
						if (lengthString == "-1") lengthString = "max";

						return string.Format("[{0}] [{1}]({2}) {3} {4}", Name, Type, lengthString, IsNullableText,
							includeDefaultConstraint ? DefaultText : string.Empty);
					case "decimal":
					case "numeric":

						return string.Format("[{0}] [{1}]({2},{3}) {4} {5}", Name, Type, Precision, Scale, IsNullableText,
							includeDefaultConstraint ? DefaultText : string.Empty);
					default:
						throw new NotSupportedException("Error scripting column " + Name + ". SQL data type " + Type + " is not supported.");
				}
			}
			return string.Format("[{0}] AS {1}", Name, ComputedDefinition);
		}

		public string ScriptCreate() {
			return ScriptBase(true);
		}

		public string ScriptAlter() {
			return ScriptBase(false);
		}

		internal static Type SqlTypeToNativeType(string sqlType) {
			switch (sqlType.ToLower()) {
				case "bit":
					return typeof (bool);
				case "datetime":
				case "smalldatetime":
					return typeof (DateTime);
				case "int":
					return typeof (int);
				case "uniqueidentifier":
					return typeof (Guid);
				case "binary":
				case "varbinary":
				case "image":
					return typeof (byte[]);
				default:
					return typeof (string);
			}
		}

		public Type SqlTypeToNativeType() {
			return SqlTypeToNativeType(Type);
		}
	}

	public class ColumnDiff {
		public Column Source;
		public Column Target;

		public ColumnDiff(Column target, Column source) {
			Source = source;
			Target = target;
		}

		public bool IsDiff {
			get { return IsDiffBase || DefaultIsDiff; }
		}

		private bool IsDiffBase {
			get {
				return Source.IsNullable != Target.IsNullable || Source.Length != Target.Length ||
				       Source.Position != Target.Position || Source.Type != Target.Type || Source.Precision != Target.Precision ||
				       Source.Scale != Target.Scale || Source.ComputedDefinition != Target.ComputedDefinition;
			}
		}

		public bool DefaultIsDiff {
			get { return Source.DefaultText != Target.DefaultText; }
		}

		public bool OnlyDefaultIsDiff {
			get { return DefaultIsDiff && !IsDiffBase; }
		}

		/*public string Script() {
			return Target.Script();
		}*/
	}
}
