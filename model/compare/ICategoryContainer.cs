using System.Collections.Generic;

namespace model.compare {
    public interface ICategoryContainer {
        List<Category> Categories { get; set; }

        string Script();
    }
}