using System.Xml.Serialization;

namespace model {
	public class Identity {
		[XmlAttribute]
		public string Increment;
		[XmlAttribute]
		public string Seed;

		private Identity() { }

		public Identity(string seed, string increment) {
			Seed = seed;
			Increment = increment;
		}

		public Identity(int seed, int increment) {
			Seed = seed.ToString();
			Increment = increment.ToString();
		}

		public string Script() {
			return string.Format("IDENTITY ({0},{1})", Seed, Increment);
		}
	}
}