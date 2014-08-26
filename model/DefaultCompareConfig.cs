namespace model {
    public class DefaultCompareConfig : ICompareConfig {
        public virtual CompareMethod RoutinesCompareMethod { get { return CompareMethod.FindAllDifferences; } }
        public virtual CompareMethod TablesCompareMethod { get { return CompareMethod.FindAllDifferences; } }
        public virtual CompareMethod ColumnsCompareMethod { get { return CompareMethod.FindAllDifferences; } }
        public virtual CompareMethod ForeignKeysCompareMethod { get { return CompareMethod.FindAllDifferences; } }

        public virtual bool IgnoreProps { get { return false; } }
        public virtual bool IgnoreDefaultsNameMismatch { get { return true; } }
    }
}