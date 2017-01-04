namespace SchemaZen.Library.Models {
	public class Identity {
		public string Increment { get; set; }
		public string Seed { get; set; }

        public Identity(string seed, string increment) {
			Seed = seed;
			Increment = increment;
		}

		public Identity(int seed, int increment) {
			Seed = seed.ToString();
			Increment = increment.ToString();
		}

		public string Script() {
			return $"IDENTITY ({Seed},{Increment})";
		}
	}
}
