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
            report.Categories[0].Name.Should().Be("Tables");
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

        [Test]
        public void CanAddCategoryWithAddedForeinKey() {
            var diff = new DatabaseDiff();
            diff.ForeignKeysAdded.Add(new ForeignKey("My_FK"));

            var report = diff.CreateDiffReport();

            report.Categories.Count.Should().Be(1);
            report.Categories[0].Name.Should().Be("Foreign Keys");
        }

        [Test]
        public void CanAddCategoryWithDeletedForeinKey()
        {
            var diff = new DatabaseDiff();
            diff.ForeignKeysDeleted.Add(new ForeignKey("My_FK"));

            var report = diff.CreateDiffReport();

            report.Categories.Count.Should().Be(1);
        }

        [Test]
        public void CanAddCategoryWithChangedForeinKey()
        {
            var diff = new DatabaseDiff();
            diff.ForeignKeysDiff.Add(new ForeignKey("My_FK"));

            var report = diff.CreateDiffReport();

            report.Categories.Count.Should().Be(1);
        }

        [Test]
        public void CanAddCategoryWithAddedRoutine()
        {
            var diff = new DatabaseDiff();
            diff.RoutinesAdded.Add(new Routine("dbo","MySP"));

            var report = diff.CreateDiffReport();

            report.Categories.Count.Should().Be(1);
            report.Categories[0].Name.Should().Be("Routines");
        }

        [Test]
        public void CanAddCategoryWithDeletedRoutine()
        {
            var diff = new DatabaseDiff();
            diff.RoutinesDeleted.Add(new Routine("dbo", "MySP"));

            var report = diff.CreateDiffReport();

            report.Categories.Count.Should().Be(1);
        }

        [Test]
        public void CanAddCategoryWithChangedRoutine()
        {
            var diff = new DatabaseDiff();
            diff.RoutinesDiff.Add(new Routine("dbo", "MySP"));

            var report = diff.CreateDiffReport();

            report.Categories.Count.Should().Be(1);
        }
    }
}