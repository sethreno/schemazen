namespace SchemaZen.model {
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
						: string.Format("EXEC('ALTER DATABASE [' + @DB + '] COLLATE {0}')", Value);

				case "COMPATIBILITY_LEVEL":
					return string.IsNullOrEmpty(Value) ? string.Empty : string.Format("EXEC dbo.sp_dbcmptlevel @DB, {0}", Value);

				default:
					return string.IsNullOrEmpty(Value)
						? string.Empty
						: string.Format("EXEC('ALTER DATABASE [' + @DB + '] SET {0} {1}')", Name, Value);
			}
		}
	}
}
