using System;

namespace SchemaZen.Library {
	public class StringUtil {
		/// <summary>
		///     Adds a space to the beginning of a string.
		///     If the string is null or empty it's returned as-is
		/// </summary>
		public static string AddSpaceIfNotEmpty(string val) {
			if (string.IsNullOrEmpty(val)) return val;
			return $" {val}";
		}

		/// <summary>
		///     Converts an array of 8-bit unsigned integers to its equivalent string representation that is encoded with uppercase hex characters.
		///     This method acts as a proxy between different framework implementations
		/// </summary>
		public static byte[] FromHexString(string s) {
#if NET5_0_OR_GREATER
			return Convert.FromHexString(s);
#else
			return HexStringToByteArrayV5_3(s);
#endif
		}

		/// <summary>
		/// From: https://stackoverflow.com/a/68066131/198452
		/// </summary>
		private static byte[] HexStringToByteArrayV5_3(string hexString) {
			int hexStringLength = hexString.Length;
			byte[] b = new byte[hexStringLength / 2];
			for (int i = 0; i < hexStringLength; i += 2) {
				int topChar = hexString[i];
				topChar = (topChar > 0x40 ? (topChar & ~0x20) - 0x37 : topChar - 0x30) << 4;
				int bottomChar = hexString[i + 1];
				bottomChar = bottomChar > 0x40 ? (bottomChar & ~0x20) - 0x37 : bottomChar - 0x30;
				b[i / 2] = (byte)(topChar + bottomChar);
			}
			return b;
		}


		/// <summary>
		///     Converts the specified string, which encodes binary data as hex characters, to an equivalent 8-bit unsigned integer array.
		///     This method acts as a proxy between different framework implementations
		/// </summary>
		public static string ToHexString(byte[] inArray)
		{
#if NET5_0_OR_GREATER
			return Convert.ToHexString(inArray);
#else
			return ByteArrayToHex(inArray);
#endif
		}

		/// <summary>
		/// From: https://stackoverflow.com/a/632920/198452
		/// </summary>
		private static string ByteArrayToHex(byte[] barray)
		{
			char[] c = new char[barray.Length * 2];
			byte b;
			for (int i = 0; i < barray.Length; ++i)
			{
				b = ((byte)(barray[i] >> 4));
				c[i * 2] = (char)(b > 9 ? b + 0x37 : b + 0x30);
				b = ((byte)(barray[i] & 0xF));
				c[i * 2 + 1] = (char)(b > 9 ? b + 0x37 : b + 0x30);
			}
			return new string(c);
		}
	}

	namespace Extensions {
		/// <summary>
		///     Extension methods to make sql script generators more readable.
		/// </summary>
		public static class Strings {
			public static string Space(this string val) {
				return StringUtil.AddSpaceIfNotEmpty(val);
			}
		}
	}
}
