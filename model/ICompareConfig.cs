namespace model{
    public interface ICompareConfig {
        bool CompareColumnPosition { get; }
        ConstraintCompareMethod ContraCompareMethod { get; }
    }

    public enum ConstraintCompareMethod {
        Name, Columns
    }
}