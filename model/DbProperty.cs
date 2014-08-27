using System.Xml.Serialization;

namespace model {
	public class DbProp {
		public Database _db;

		private DbProp()
		{
		}

		public DbProp(string name, string value) {
			Name = name;
			Value = value;
		}

		[XmlAttribute]
		public string Name { get; set; }
		[XmlAttribute]
		public string Value { get; set; }

		public string Script() {
			switch (Name.ToUpper()) {
				case "COLLATE":
					if (string.IsNullOrEmpty(Value)) return "";
					return string.Format("EXEC('ALTER DATABASE [' + @DB + '] COLLATE {0}')",
						Value);

				case "COMPATIBILITY_LEVEL":
					if (string.IsNullOrEmpty(Value)) return "";
					return string.Format("EXEC dbo.sp_dbcmptlevel @DB, {0}", Value);

				default:
					if (string.IsNullOrEmpty(Value)) return "";
					return string.Format("EXEC('ALTER DATABASE [' + @DB + '] SET {0} {1}')",
						Name, Value);
			}
		}
	}
}