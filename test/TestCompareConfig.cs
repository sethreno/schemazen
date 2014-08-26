using model;

namespace test {
    public class TestCompareConfig : ICompareConfig {
        public CompareMethod RoutinesCompareMethod { get { return CompareMethod.FindButIgnoreAdditionalItems; } }
    }
}