using System.Collections.Generic;
using System.Xml.Serialization;

namespace model.compare {
    public class Category {
        public Category() {
            Entries = new List<DiffEntry>();
        }

        [XmlAttribute]
        public string Name { get; set; }

        public List<DiffEntry> Entries { get; set; }

        public DiffEntry AddEntry(string name, DiffEntryType diffEntryType) {
            var entry = new DiffEntry {Name = name, Type = diffEntryType};
            Entries.Add(entry);

            return entry;
        }
    }
}