using System.Collections.Generic;
using System.Xml.Serialization;

namespace model.compare {
    public class DiffEntry {
        public DiffEntry() {
            Categories = new List<Category>();
        }

        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public DiffEntryType Type { get; set; }

        public string Details { get; set; }

        public List<Category> Categories { get; set; }
    }

    public enum DiffEntryType {
        Added, Deleted, Changed
    }
}