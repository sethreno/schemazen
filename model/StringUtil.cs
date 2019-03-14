using System;

namespace SchemaZen.Library {

	public class StringUtil {

		/// <summary>
		/// Adds a space to the beginning of a string.
		/// If the string is null or empty it's returned as-is
		/// </summary>
		public static string AddSpaceIfNotEmpty(string val) {
			if (String.IsNullOrEmpty(val)) return val;
			return $" {val}";
		}
	}

	namespace Extensions {

		/// <summary>
		/// Extension methods to make sql script generators more readable.
		/// </summary>
		public static class Strings {

			public static string Space(this String val) {
				return StringUtil.AddSpaceIfNotEmpty(val);
			}
		}
	}
}
