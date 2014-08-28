using System.Collections.Generic;

namespace model.compare {
    public abstract class CategoryContainerBase : ICategoryContainer{
        public List<Category> Categories { get; set; }

        public Category AddCategory(string name) {
            Categories = Categories ?? new List<Category>();

            var category = new Category {Name = name};
            Categories.Add(category);

            return category;
        }
    }
}