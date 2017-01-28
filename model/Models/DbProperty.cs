namespace SchemaZen.Library.Models {
	public class DbProp {
		public DbProp(string name, string value) {
			Name = name;
			Value = value;
		}

		public string Name { get; set; }
		public string Value { get; set; }

		public string Script() {
			switch (Name.ToUpper()) {
				case "COLLATE":
					return string.IsNullOrEmpty(Value)
						? string.Empty
						: $"EXEC('ALTER DATABASE [' + @DB + '] COLLATE {Value}')";

				case "COMPATIBILITY_LEVEL":
					return string.IsNullOrEmpty(Value) ? string.Empty : $"EXEC dbo.sp_dbcmptlevel @DB, {Value}";

				default:
					return string.IsNullOrEmpty(Value)
						? string.Empty
						: $"EXEC('ALTER DATABASE [' + @DB + '] SET {Name} {Value}')";
			}
		}
	}
}
