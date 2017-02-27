using System;
using System.Collections.Generic;

namespace SchemaZen.Library.Models.Comparers
{
    public class ForeignKeyComparer : IComparer<ForeignKey>
    {
        public static ForeignKeyComparer Instance { get; } = new ForeignKeyComparer();

        public int Compare(ForeignKey x, ForeignKey y)
        {
            var result = string.Compare(x.Table.Owner, y.Table.Owner, StringComparison.Ordinal);

            if (result != 0)
            {
                return result;
            }

            result = string.Compare(x.Table.Name, y.Table.Name, StringComparison.Ordinal);

            if (result != 0)
            {
                return result;
            }

            result = x.Columns.Count.CompareTo(y.Columns.Count);

            if (result != 0)
            {
                return result;
            }

            for (var i = 0; i < x.Columns.Count; i++)
            {
                result = string.Compare(x.Columns[i], y.Columns[i], StringComparison.Ordinal);
                if (result != 0)
                {
                    return result;
                }
            }

            result = x.RefColumns.Count.CompareTo(y.RefColumns.Count);

            if (result != 0)
            {
                return result;
            }

            for (var i = 0; i < x.RefColumns.Count; i++)
            {
                result = string.Compare(x.RefColumns[i], y.RefColumns[i], StringComparison.Ordinal);
                if (result != 0)
                {
                    return result;
                }
            }

            return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        }
    }
}
