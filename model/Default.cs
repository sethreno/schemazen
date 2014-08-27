using System.Xml.Serialization;

namespace model {
	public class Default {
		[XmlAttribute]
		public string Name;
		[XmlAttribute]
		public string Value;

		private Default() { }

		public Default(string name, string value) {
			Name = name;
			Value = value;
		}

		public string Script() {
			return string.Format("CONSTRAINT [{0}] DEFAULT {1}", Name, Value);
		}
	}
}