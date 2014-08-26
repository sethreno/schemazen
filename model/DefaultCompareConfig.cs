namespace model {
    public class DefaultCompareConfig : ICompareConfig {
        public virtual CompareMethod RoutinesCompareMethod { get { return CompareMethod.FindAllDifferences; } }
        public virtual bool IgnoreProps { get { return false; } }
    }
}