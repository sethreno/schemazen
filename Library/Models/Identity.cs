namespace SchemaZen.Library.Models; 

public class Identity {
	public Identity(string seed, string increment) {
		Seed = seed;
		Increment = increment;
	}

	public Identity(int seed, int increment) {
		Seed = seed.ToString();
		Increment = increment.ToString();
	}

	public string Increment { get; set; }
	public string Seed { get; set; }

	public string Script() {
		return $"IDENTITY ({Seed},{Increment})";
	}
}
