using FluentAssertions;
using model;
using NUnit.Framework;

namespace test.compare {
    public class DiffReportTests {
        [Test]
        public void CanCreateDiffReport() {
            var databaseDiff = new DatabaseDiff();

            var report = databaseDiff.CreateDiffReport();

            report.Should().NotBeNull();
        }

        [Test]
        public void CanAddCategoryWithAddedTable() {
            var diff = new DatabaseDiff();
            diff.TablesAdded.Add(new Table("dbo", "MyTable"));

            var report = diff.CreateDiffReport();

            report.Categories.Count.Should().Be(1);
        }

        [Test]
        public void CanAddCategoryWithDeletedTable() {
            var diff = new DatabaseDiff();
            diff.TablesDeleted.Add(new Table("dbo", "MyTable"));

            var report = diff.CreateDiffReport();

            report.Categories.Count.Should().Be(1);
        }

        [Test]
        public void CanAddCategoryWithChangedTable()
        {
            var diff = new DatabaseDiff();
            diff.TablesDiff.Add(new TableDiff());

            var report = diff.CreateDiffReport();

            report.Categories.Count.Should().Be(1);
        }
    }
}