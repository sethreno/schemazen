using System;
using System.Configuration;

namespace SchemaZen.Tests {
	public class ConfigHelper {
		public static string TestDB => GetSetting("testdb");

	    public static string TestSchemaDir => GetSetting("test_schema_dir");

	    public static string SqlDbDiffPath => GetSetting("SqlDbDiffPath");

	    private static string GetSetting(string key) {
			var val = Environment.GetEnvironmentVariable(key);
			return val ?? ConfigurationManager.AppSettings[key];
		}
	}
}
