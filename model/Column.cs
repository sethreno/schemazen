using System;

namespace model {
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

		public Column() {
		}

		public Column(string name, string type, bool @null, Default @default) {
			this.Name = name;
			this.Type = type;
			this.Default = @default;
		}

		public Column(string name, string type, int length, bool @null, Default @default)
			: this(name, type, @null, @default) {
			this.Length = length;
		}

		public Column(string name, string type, byte precision, int scale, bool @null, Default @default)
			: this(name, type, @null, @default) {
			this.Precision = precision;
			this.Scale = scale;
		}

		private string IsNullableText {
			get {
				if (this.IsNullable) return "NULL";
				return "NOT NULL";
			}
		}

		public string DefaultText {
			get {
				if (this.Default == null) return "";
				return "\r\n      " + this.Default.Script();
			}
		}

		public string IdentityText {
			get {
				if (this.Identity == null) return "";
				return "\r\n      " + this.Identity.Script();
			}
		}

		public ColumnDiff Compare(Column c) {
			return new ColumnDiff(this, c);
		}

		public string Script() {
			switch (this.Type) {
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

					return string.Format("[{0}] [{1}] {2} {3} {4}", this.Name, this.Type, this.IsNullableText, this.DefaultText, this.IdentityText);
				case "binary":
				case "char":
				case "nchar":
				case "nvarchar":
				case "varbinary":
				case "varchar":
					var lengthString = this.Length.ToString();
					if (lengthString == "-1") lengthString = "max";

					return string.Format("[{0}] [{1}]({2}) {3} {4}", this.Name, this.Type, lengthString, this.IsNullableText, this.DefaultText);
				case "decimal":
				case "numeric":

					return string.Format("[{0}] [{1}]({2},{3}) {4} {5}", this.Name, this.Type, this.Precision, this.Scale, this.IsNullableText, this.DefaultText);
				default:
					throw new NotSupportedException("SQL data type " + this.Type + " is not supported.");
			}
		}
	}

	public class ColumnDiff {
		public Column Source;
		public Column Target;

		public ColumnDiff(Column target, Column source) {
			this.Source = source;
			this.Target = target;
		}

		public bool IsDiff {
			get {
				return this.Source.DefaultText != this.Target.DefaultText || this.Source.IsNullable != this.Target.IsNullable || this.Source.Length != this.Target.Length || this.Source.Position != this.Target.Position || this.Source.Type != this.Target.Type || this.Source.Precision != this.Target.Precision || this.Source.Scale != this.Target.Scale;
			}
		}

		public string Script() {
			return this.Target.Script();
		}
	}
}