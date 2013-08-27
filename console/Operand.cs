using model;

namespace console {
	public enum OpType {
		Database,
		ScriptDir
	}

	public class Operand {
		public OpType OpType;
		public string Value;

		public static Operand Parse(string text) {
			if (string.IsNullOrEmpty(text)) return null;
			var obj = new Operand();
			if (text.StartsWith("cn:")) {
				obj.OpType = OpType.Database;
				obj.Value = text.Substring(3);
			}
			else if (text.StartsWith("as:")) {
				obj.OpType = OpType.Database;
				obj.Value = ConfigHelper.GetAppSetting(text.Substring(3));
			}
			else if (text.StartsWith("cs:")) {
				obj.OpType = OpType.Database;
				obj.Value = ConfigHelper.GetConnectionString(text.Substring(3));
			}
			else if (text.StartsWith("dir:")) {
				obj.OpType = OpType.ScriptDir;
				obj.Value = text.Substring(4);
			}
			else {
				return null;
			}
			return obj;
		}
	}
}