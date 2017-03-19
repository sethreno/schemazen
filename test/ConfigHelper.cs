using System;
using System.Configuration;
using System.IO;

namespace SchemaZen.Tests {
	public class ConfigHelper {
		public static string TestDB => GetSetting("testdb");

		public static string TestSchemaDir => Path.Combine(AssemblyDirectory, "test_schemas");

        public static string SqlDbDiffPath => GetSetting("SqlDbDiffPath");

		private static string GetSetting(string key) {
			var val = Environment.GetEnvironmentVariable(key);
			return val ?? ConfigurationManager.AppSettings[key];
		}

        protected internal static string AssemblyDirectory
        {
            get
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }
    }
}
