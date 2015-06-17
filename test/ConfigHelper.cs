using System.Configuration;

namespace test {
	public class ConfigHelper {
		public static string TestDB {
			get { return ConfigurationManager.AppSettings["testdb"]; }
		}

		public static string TestSchemaDir {
			get { return ConfigurationManager.AppSettings["test_schema_dir"]; }
		}

		public static string SqlDbDiffPath {
			get { return ConfigurationManager.AppSettings["SqlDbDiffPath"]; }
		}
	}
}