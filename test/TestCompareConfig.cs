using model;

namespace test {
    public class TestCompareConfig : ICompareConfig {
        public bool CompareColumnPosition { get { return true; } }
        public ConstraintCompareMethod ContraCompareMethod { get{return ConstraintCompareMethod.Name;} }
    }
}