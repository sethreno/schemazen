namespace model {
    public class CompareConfig {
        public CompareConfig() {
            RoutinesCompareMethod = CompareMethod.FindAllDifferences;
            TablesCompareMethod = CompareMethod.FindAllDifferences;
            ColumnsCompareMethod = CompareMethod.FindAllDifferences;
            ForeignKeysCompareMethod = CompareMethod.FindAllDifferences;
            ConstraintsCompareMethod = CompareMethod.FindAllDifferences;

            IgnoreProps = false;
            IgnoreDefaultsNameMismatch = false;
            IgnoreRoutinesTextMismatch = false;
        }

        public CompareMethod RoutinesCompareMethod { get; set; }
        public CompareMethod TablesCompareMethod { get; set; }
        public CompareMethod ColumnsCompareMethod { get; set; }
        public CompareMethod ForeignKeysCompareMethod { get; set; }
        public CompareMethod ConstraintsCompareMethod { get; set; }

        public bool IgnoreProps { get; set; }
        public bool IgnoreDefaultsNameMismatch { get; set; }
        public bool IgnoreRoutinesTextMismatch { get; set; }
        public bool IgnoreConstraintsNameMismatch { get; set; }
    }

    public enum CompareMethod
    {
        Ignore, FindAllDifferences, FindButIgnoreAdditionalItems
    }
}