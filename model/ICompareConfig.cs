namespace model{
    public interface ICompareConfig {
        CompareMethod RoutinesCompareMethod { get; }
        bool IgnoreProps { get; }
    }

    public enum CompareMethod {
        Ignore, FindAllDifferences, FindButIgnoreAdditionalItems
    }
}