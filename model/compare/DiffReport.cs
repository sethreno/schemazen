using System.Collections.Generic;

namespace model.compare {
    public class DiffReport {
        public DiffReport() {
            Categories = new List<Category>();
        }

        public List<Category> Categories { get; set; }
    }
}