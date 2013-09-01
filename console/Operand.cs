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
			if (text.Contains("=")) {
				obj.OpType = OpType.Database;
				obj.Value = text;
			}
			else {
				obj.OpType = OpType.ScriptDir;
				obj.Value = text;
			}
			return obj;
		}
	}
}