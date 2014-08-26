namespace model{
    public interface ICompareConfig {
        CompareMethod RoutinesCompareMethod { get; }
        bool IgnoreProps { get; }
    }

    public enum CompareMethod {
        Ignore, FindAllDifferences, FindButIgnoreAdditionalItems
    }

    public class DefaultCompareConfig : ICompareConfig {
        public virtual CompareMethod RoutinesCompareMethod { get { return CompareMethod.FindAllDifferences; } }
        public virtual bool IgnoreProps { get { return false; } }
    }
}