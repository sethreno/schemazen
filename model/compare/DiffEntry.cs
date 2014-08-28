using System.ComponentModel;
using System.Xml.Serialization;

namespace model.compare {
    public class DiffEntry : CategoryContainerBase, ICategoryContainer {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public DiffEntryType Type { get; set; }

        [DefaultValue(null)]
        public string Details { get; set; }
    }

    public enum DiffEntryType {
        Added, Deleted, Changed
    }
}