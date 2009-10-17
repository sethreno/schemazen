
namespace model {
	public class Identity {
		public int Seed;
		public int Increment;

		public Identity(int seed, int increment) {
			this.Seed = seed;
			this.Increment = increment;
		}

		public string Script() {
			return string.Format("IDENTITY ({0},{1})", Seed, Increment);
		}
	}
}
