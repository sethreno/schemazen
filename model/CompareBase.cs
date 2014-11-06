using System;
using System.Collections.Generic;
using System.Linq;

namespace model {
    public abstract class CompareBase {
        protected void CheckSource<T>(CompareMethod setting, IEnumerable<T> elements, Func<T, T> getOther,
            Action<T> setAsAdded, Action<T, T> checkIfChanged) where T : class {
            if (setting != CompareMethod.FindAllDifferences && setting != CompareMethod.FindButIgnoreAdditionalItems)
                return;

            foreach (var element in elements) {
                var other = getOther(element);

                if (other == null)
                    setAsAdded(element);
                else
                    checkIfChanged(element, other);
            }
        }

        protected void CheckTarget<T>(CompareMethod setting, IEnumerable<T> elements, Func<T, bool> existsOnlyInTarget,
            Action<T> setAsDeleted) {
            if (setting != CompareMethod.FindAllDifferences) return;

            foreach (var element in elements.Where(existsOnlyInTarget)) {
                setAsDeleted(element);
            };
        }
    }
}