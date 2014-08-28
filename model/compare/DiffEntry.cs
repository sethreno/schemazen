namespace model.compare {
    public class DiffEntry {
        public string Name { get; set; }
        public string Details { get; set; }

        public DiffEntryType Type { get; set; }
    }

    public enum DiffEntryType {
        Added, Deleted, Changed
    }
}