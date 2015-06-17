namespace model {
	public class Identity {
		public string Increment;
		public string Seed;

		public Identity(string seed, string increment) {
			this.Seed = seed;
			this.Increment = increment;
		}

		public Identity(int seed, int increment) {
			this.Seed = seed.ToString();
			this.Increment = increment.ToString();
		}

		public string Script() {
			return string.Format("IDENTITY ({0},{1})", this.Seed, this.Increment);
		}
	}
}