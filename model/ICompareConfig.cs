namespace model{
    public interface ICompareConfig {
        CompareMethod RoutinesCompareMethod { get; }
    }

    public enum CompareMethod {
        Ignore, FindAllDifferences, FindButIgnoreAdditionalItems
    }
}