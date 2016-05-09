using System.Configuration;

namespace SchemaZen.Library.Models {
	public class ConfigHelper {
		public static string GetConnectionString(string name) {
			return ConfigurationManager.ConnectionStrings[name].ConnectionString;
		}

		public static string GetAppSetting(string key) {
			return ConfigurationManager.AppSettings[key];
		}
	}
}
