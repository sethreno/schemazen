using model;

namespace console {
    public class DefaultCompareCompareConfig : ICompareConfig {
        public bool CompareColumnPosition { get { return true; } }
        public ConstraintCompareMethod ContraCompareMethod { get{return ConstraintCompareMethod.Name;} }
    }
}