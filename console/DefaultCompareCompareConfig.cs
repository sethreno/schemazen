using model;

namespace console {
    public class DefaultCompareCompareConfig : ICompareConfig {
        public CompareMethod RoutinesCompareMethod { get { return CompareMethod.FindButIgnoreAdditionalItems; } }
    }
}