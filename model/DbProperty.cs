namespace model {
	public class DbProp {
		public Database _db;

		public DbProp(string name, string value) {
			this.Name = name;
			this.Value = value;
		}

		public string Name { get; set; }
		public string Value { get; set; }

		public string Script() {
			switch (this.Name.ToUpper()) {
				case "COLLATE":
					if (string.IsNullOrEmpty(this.Value)) return "";
					return string.Format("EXEC('ALTER DATABASE [' + @DB + '] COLLATE {0}')", this.Value);

				case "COMPATIBILITY_LEVEL":
					if (string.IsNullOrEmpty(this.Value)) return "";
					return string.Format("EXEC dbo.sp_dbcmptlevel @DB, {0}", this.Value);

				default:
					if (string.IsNullOrEmpty(this.Value)) return "";
					return string.Format("EXEC('ALTER DATABASE [' + @DB + '] SET {0} {1}')", this.Name, this.Value);
			}
		}
	}
}