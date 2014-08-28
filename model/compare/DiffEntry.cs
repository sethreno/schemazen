using System.Collections.Generic;

namespace model.compare {
    public class DiffEntry {
        public DiffEntry() {
            Categories = new List<Category>();
        }

        public string Name { get; set; }
        public string Details { get; set; }

        public DiffEntryType Type { get; set; }

        public List<Category> Categories { get; set; }
    }

    public enum DiffEntryType {
        Added, Deleted, Changed
    }
}