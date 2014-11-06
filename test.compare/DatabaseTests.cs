using System.Linq;
using FluentAssertions;
using model;
using NUnit.Framework;

namespace test.compare
{
    public class DatabaseTests
    {
        [Test]
        public void CanIgnoreTable()
        {
            var db = new Database();
            var table = new Table("dbo", "IgnoredTableWithConstraints");
            var constraint = new Constraint("TestConstraint", "", "");
            table.Constraints.Add(constraint);
            db.DataTables.Add(table);

            db.Ignore(new[] { "IgnoredTableWithConstraints" });

            db.Tables.Any().Should().BeFalse();
        }

        [Test]
        public void CanIgnoreForeignKeysOfIgnoredTable()
        {
            var db = new Database();
            var table = new Table("dbo", "IgnoredTableWithForeignKey");
            var refTable = new Table("dbo", "RefTable");
            db.DataTables.Add(table);
            db.ForeignKeys.Add(new ForeignKey(table,"","", refTable, ""));

            db.Ignore(new[] { "IgnoredTableWithForeignKey" });

            db.Tables.Any().Should().BeFalse();
            db.ForeignKeys.Any().Should().BeFalse();
        }
    }
}