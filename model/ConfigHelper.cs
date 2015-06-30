using System.Configuration;

namespace SchemaZen.model {
	public class ConfigHelper {
		public static string GetConnectionString(string name) {
			return ConfigurationManager.ConnectionStrings[name].ConnectionString;
		}

		public static string GetAppSetting(string key) {
			return ConfigurationManager.AppSettings[key];
		}
	}
}
