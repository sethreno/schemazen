using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace model.compare {
    public class Category {
        public Category() {
            Entries = new List<DiffEntry>();
        }

        [XmlAttribute]
        public string Name { get; set; }

        public List<DiffEntry> Entries { get; set; }

        public DiffEntry AddEntry(string name, DiffEntryType diffEntryType, string details = null) {
            var entry = new DiffEntry {Name = name, Type = diffEntryType, Details = details};
            Entries.Add(entry);

            return entry;
        }

        public string Script() {
            var stringBuilder = new StringBuilder();
            foreach (var diffEntry in Entries) {
                stringBuilder.Append(diffEntry.Details);
            }

            return stringBuilder.ToString();
        }
    }
}