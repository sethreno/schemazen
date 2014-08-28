using System.Collections.Generic;

namespace model.compare {
    public class Category {
        public Category() {
            Entries = new List<DiffEntry>();
        }

        public string Name { get; set; }

        public List<DiffEntry> Entries { get; set; }
    }
}