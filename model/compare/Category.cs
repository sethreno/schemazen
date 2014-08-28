using System.Collections.Generic;

namespace model.compare {
    public class Category {
        public string Name { get; set; }

        public DiffEntry[] Entries { get; set; }
    }
}