namespace model {
	public class Default {
		public string Name;
		public string Value;

		public Default(string name, string value) {
			this.Name = name;
			this.Value = value;
		}

		public string Script() {
			return string.Format("CONSTRAINT [{0}] DEFAULT {1}", this.Name, this.Value);
		}
	}
}