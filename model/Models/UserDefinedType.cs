using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SchemaZen.Library.Models {

	public class UserDefinedType : INameable, IHasOwner, IScriptable {

		public string Name { get; set; }

		public string Owner { get; set; }

		public string BaseTypeName { get; set; }

		public string MaxLength {
			get {
				switch (BaseTypeName) {
					case "nvarchar":
						return _maxLength == -1 ? "max" : ((short)(_maxLength / 2)).ToString();
					case "varchar":
					case "binary":
					case "char":
					case "nchar":
					case "varbinary":
						return _maxLength == -1 ? "max" : _maxLength.ToString();
					default:
						return _maxLength.ToString();
				}
			}
		}
		private readonly short _maxLength;

		private bool HasMaxLength() {
			switch (BaseTypeName) {
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
					return false;
				case "decimal":
				case "numberic":
					throw new Exception("Precision and Scale not handled yet.");
				default:
					return true;
			}
		}

		public string Nullable => _nullable ? "NULL" : "NOT NULL";
		private readonly bool _nullable;

		public UserDefinedType(string owner,
							   string name,
							   string baseTypeName,
							   short maxLength,
							   bool nullable) {
			Owner = owner;
			Name = name;
			BaseTypeName = baseTypeName;
			_maxLength = maxLength;
			_nullable = nullable;
		}

		public string ScriptCreate() {
			var text = new StringBuilder();

			text.Append($"CREATE TYPE [{Owner}].[{Name}] FROM [{BaseTypeName}]");

			if (HasMaxLength()) {
				text.Append($" ({MaxLength})");
			}

			text.Append($" {Nullable}");

			return text.ToString();
		}

	}

}
