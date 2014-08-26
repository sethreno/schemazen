namespace model{
    public interface ICompareConfig {
        CompareMethod RoutinesCompareMethod { get; }
        bool IgnoreProps { get; }
        bool IgnoreDefaultsNameMismatch { get; }
    }

    public enum CompareMethod {
        Ignore, FindAllDifferences, FindButIgnoreAdditionalItems
    }
}