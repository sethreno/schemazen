using System.Collections.Generic;
using System.Text;

namespace model.compare {
    public abstract class CategoryContainerBase : ICategoryContainer{
        public List<Category> Categories { get; set; }

        public string Script() {
            var stringbuilder = new StringBuilder();
            foreach (var category in Categories) {
                stringbuilder.Append(category.Script());
            }

            return stringbuilder.ToString();
        }

        public Category AddCategory(string name) {
            Categories = Categories ?? new List<Category>();

            var category = new Category {Name = name};
            Categories.Add(category);

            return category;
        }
    }
}