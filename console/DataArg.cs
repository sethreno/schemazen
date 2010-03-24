namespace console {
    public class DataArg {

        private string _value = "";
        public string Value {
            get { return _value; }
        }
        public static DataArg Parse(string[] args) {
            DataArg obj = null;
            for (int i = 0; i < args.Length; i++) {
                var arg = args[i];
                if (arg.ToLower().StartsWith("--data")) {
                    if (arg.Length > "--data".Length) {
                        obj = new DataArg();
                        obj._value = arg.Substring(6);
                    } else {
                        if (i + 1 < args.Length) {
                            obj = new DataArg();
                            obj._value = args[i + 1];
                        }
                    }
                }
            }
            return obj;
        }
    }
}
