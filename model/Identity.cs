namespace model {
	public class Identity {
		public string Increment;
		public string Seed;

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
