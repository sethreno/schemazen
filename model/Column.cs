using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace model {
	public class Column {
		public Default Default;
		public Identity Identity;

		[XmlAttribute]
		[DefaultValue(false)]
		public bool IsNullable;
		[XmlAttribute]
		[DefaultValue(0)]
		public int Length;
		[XmlAttribute]
		public string Name;
		[XmlAttribute]
		[DefaultValue(0)]
		public int Position;
		[XmlAttribute]
		[DefaultValue(0)]
		public byte Precision;
		[XmlAttribute]
		[DefaultValue(0)]
		public int Scale;
		[XmlAttribute]
		public string Type;

		public Column() {
		}

		public Column(string name, string type, bool @null, Default @default) {
			Name = name;
			Type = type;
			Default = @default;
		}

		public Column(string name, string type, int length, bool @null, Default @default)
			: this(name, type, @null, @default) {
			Length = length;
		}

		public Column(string name, string type, byte precision, int scale, bool @null, Default @default)
			: this(name, type, @null, @default) {
			Precision = precision;
			Scale = scale;
		}

		private string IsNullableText {
			get {
				if (IsNullable) return "NULL";
				return "NOT NULL";
			}
		}

		public string DefaultText {
			get {
				if (Default == null) return "";
				return "\r\n      " + Default.Script();
			}
		}

		public string IdentityText {
			get {
				if (Identity == null) return "";
				return "\r\n      " + Identity.Script();
			}
		}

		public ColumnDiff Compare(Column c, CompareConfig compareConfig) {
			return new ColumnDiff(this, c, compareConfig);
		}

		public string Script() {
			switch (Type) {
				case "bigint":
				case "bit":
				case "date":
				case "datetime":
				case "datetime2":
				case "datetimeoffset":
				case "float":
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
				case "xml":

					return string.Format("[{0}] [{1}] {2} {3} {4}", Name, Type, IsNullableText, DefaultText, IdentityText);
				case "binary":
				case "char":
				case "nchar":
				case "nvarchar":
				case "varbinary":
				case "varchar":
					string lengthString = Length.ToString();
					if (lengthString == "-1") lengthString = "max";

					return string.Format("[{0}] [{1}]({2}) {3} {4}", Name, Type, lengthString, IsNullableText, DefaultText);
				case "decimal":
				case "numeric":

					return string.Format("[{0}] [{1}]({2},{3}) {4} {5}", Name, Type, Precision, Scale, IsNullableText, DefaultText);
				default:
					throw new NotSupportedException("SQL data type " + Type + " is not supported.");
			}
		}
	}

	public class ColumnDiff {
		private readonly CompareConfig _compareConfig;

		public Column Source;
		public Column Target;

		private ColumnDiff() { }

		public ColumnDiff(Column target, Column source, CompareConfig compareConfig) {
			_compareConfig = compareConfig;

			Source = source;
			Target = target;
		}

		public bool IsDiff {
			get {
				var defaultMismatch = false;
				if (_compareConfig.IgnoreDefaultsNameMismatch && Source.Default != null && Target.Default != null) {
					defaultMismatch = Source.Default.Value != Target.Default.Value;
				}
				else {
					defaultMismatch = Source.DefaultText != Target.DefaultText;
				}

				return  defaultMismatch || Source.IsNullable != Target.IsNullable ||
					   Source.Length != Target.Length || Source.Position != Target.Position || Source.Type != Target.Type ||
					   Source.Precision != Target.Precision || Source.Scale != Target.Scale;
			}
		}

		public string Script() {
			return Target.Script();
		}
	}
}