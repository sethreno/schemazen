using System.Linq;
using FluentAssertions;
using model;
using model.compare;
using NUnit.Framework;

namespace test.compare
{
    public class DiffReportTests
    {
        [Test]
        public void CanCreateDiffReport()
        {
            var databaseDiff = new DatabaseDiff();

            var report = databaseDiff.GetDiffReport();

            report.Should().NotBeNull();
        }

        [Test]
        public void CanAddCategoryWithAddedTable()
        {
            var diff = new DatabaseDiff();
            diff.TablesAdded.Add(new Table("dbo", "MyTable"));

            var report = diff.GetDiffReport();

            report.Categories.Count.Should().Be(1);
            report.Categories[0].Name.Should().Be("Tables");
        }

        [Test]
        public void CanAddCorrectEntryForAddedTable()
        {
            var diff = new DatabaseDiff();
            diff.TablesAdded.Add(new Table("dbo", "MyTable"));

            var report = diff.GetDiffReport();

            DiffEntry entry;
            (entry = report.Categories[0].Entries.FirstOrDefault()).Should().NotBeNull();
            entry.Name.Should().Be("MyTable");
            entry.Type.Should().Be(DiffEntryType.Added);
        }

        [Test]
        public void CanAddCategoryWithDeletedTable()
        {
            var diff = new DatabaseDiff();
            diff.TablesDeleted.Add(new Table("dbo", "MyTable"));

            var report = diff.GetDiffReport();

            report.Categories.Count.Should().Be(1);
        }

        [Test]
        public void CanAddCorrectEntryForDeletedTable()
        {
            var diff = new DatabaseDiff();
            diff.TablesDeleted.Add(new Table("dbo", "MyTable"));

            var report = diff.GetDiffReport();

            DiffEntry entry;
            (entry = report.Categories[0].Entries.FirstOrDefault()).Should().NotBeNull();
            entry.Name.Should().Be("MyTable");
            entry.Type.Should().Be(DiffEntryType.Deleted);
        }

        [Test]
        public void CanAddCategoryWithChangedTable()
        {
            var diff = new DatabaseDiff();
            diff.TablesDiff.Add(new TableDiff());

            var report = diff.GetDiffReport();

            report.Categories.Count.Should().Be(1);
        }

        [Test]
        public void CanAddCorrectEntryForChangedTable()
        {
            var diff = new DatabaseDiff();
            diff.TablesDiff.Add(new TableDiff { Name = "MyTable"});

            var report = diff.GetDiffReport();

            DiffEntry entry;
            (entry = report.Categories[0].Entries.FirstOrDefault()).Should().NotBeNull();
            entry.Name.Should().Be("MyTable");
            entry.Type.Should().Be(DiffEntryType.Changed);
        }

        [Test]
        public void CanAddCategoryToCategoryForAddedColumn()
        {
            var diff = new DatabaseDiff();
            var tableDiff = new TableDiff();
            tableDiff.ColumnsAdded.Add(new Column{Type = "text"});
            diff.TablesDiff.Add(tableDiff);

            var report = diff.GetDiffReport();

            report.Categories[0].Entries.Count.Should().Be(1);
            report.Categories[0].Entries[0].Categories.Count.Should().Be(1);
            report.Categories[0].Entries[0].Categories[0].Name.Should().Be("Columns");
        }

        [Test]
        public void CanAddCorrectEntryForAddedColumn()
        {
            var diff = new DatabaseDiff();
            var tableDiff = new TableDiff();
            tableDiff.ColumnsAdded.Add(new Column { Name = "MyColumn", Type = "text" });
            diff.TablesDiff.Add(tableDiff);

            var report = diff.GetDiffReport();

            DiffEntry entry;
            (entry = report.Categories[0].Entries[0].Categories[0].Entries.FirstOrDefault()).Should().NotBeNull();
            entry.Name.Should().Be("MyColumn");
            entry.Type.Should().Be(DiffEntryType.Added);
        }

        [Test]
        public void CanAddCategoryToCategoryForDeletedColumn()
        {
            var diff = new DatabaseDiff();
            var tableDiff = new TableDiff();
            tableDiff.ColumnsDroped.Add(new Column { Type = "text" });
            diff.TablesDiff.Add(tableDiff);

            var report = diff.GetDiffReport();

            report.Categories[0].Entries.Count.Should().Be(1);
            report.Categories[0].Entries[0].Categories.Count.Should().Be(1);
        }

        [Test]
        public void CanAddCorrectEntryForDeletedColumn()
        {
            var diff = new DatabaseDiff();
            var tableDiff = new TableDiff();
            tableDiff.ColumnsDroped.Add(new Column { Name = "MyColumn", Type = "text" });
            diff.TablesDiff.Add(tableDiff);

            var report = diff.GetDiffReport();

            DiffEntry entry;
            (entry = report.Categories[0].Entries[0].Categories[0].Entries.FirstOrDefault()).Should().NotBeNull();
            entry.Name.Should().Be("MyColumn");
            entry.Type.Should().Be(DiffEntryType.Deleted);
        }

        [Test]
        public void CanAddCategoryToCategoryForChangedColumn()
        {
            var diff = new DatabaseDiff();
            var tableDiff = new TableDiff();
            tableDiff.ColumnsDiff.Add(new ColumnDiff(new Column { Type = "text" }, new Column { Type = "text" }, new CompareConfig()));
            diff.TablesDiff.Add(tableDiff);

            var report = diff.GetDiffReport();

            report.Categories[0].Entries.Count.Should().Be(1);
            report.Categories[0].Entries[0].Categories.Count.Should().Be(1);
        }

        [Test]
        public void CanAddCorrectEntryForChangedColumn()
        {
            var diff = new DatabaseDiff();
            var tableDiff = new TableDiff();
            tableDiff.ColumnsDiff.Add(new ColumnDiff(new Column { Name = "TargetColumn", Type = "text" }, new Column { Name = "SourceColumn", Type = "text" }, new CompareConfig()));
            diff.TablesDiff.Add(tableDiff);

            var report = diff.GetDiffReport();

            DiffEntry entry;
            (entry = report.Categories[0].Entries[0].Categories[0].Entries.FirstOrDefault()).Should().NotBeNull();
            entry.Name.Should().Be("SourceColumn");
            entry.Type.Should().Be(DiffEntryType.Changed);
        }

        [Test]
        public void CanAddCategoryToCategoryForAddedConstraint()
        {
            var diff = new DatabaseDiff();
            var tableDiff = new TableDiff();
            tableDiff.ConstraintsAdded.Add(new Constraint("", "", ""));
            diff.TablesDiff.Add(tableDiff);

            var report = diff.GetDiffReport();

            report.Categories[0].Entries.Count.Should().Be(1);
            report.Categories[0].Entries[0].Categories.Count.Should().Be(1);
            report.Categories[0].Entries[0].Categories[0].Name.Should().Be("Constraints");
        }

        [Test]
        public void CanAddCorrectEntryForAddedConstraint()
        {
            var diff = new DatabaseDiff();
            var tableDiff = new TableDiff();
            tableDiff.ConstraintsAdded.Add(new Constraint("MyConstraint","",""));
            diff.TablesDiff.Add(tableDiff);

            var report = diff.GetDiffReport();

            DiffEntry entry;
            (entry = report.Categories[0].Entries[0].Categories[0].Entries.FirstOrDefault()).Should().NotBeNull();
            entry.Name.Should().Be("MyConstraint");
            entry.Type.Should().Be(DiffEntryType.Added);
        }

        [Test]
        public void CanAddCategoryToCategoryForDeletedConstraint()
        {
            var diff = new DatabaseDiff();
            var tableDiff = new TableDiff();
            tableDiff.ConstraintsDeleted.Add(new Constraint("", "", ""));
            diff.TablesDiff.Add(tableDiff);

            var report = diff.GetDiffReport();

            report.Categories[0].Entries.Count.Should().Be(1);
            report.Categories[0].Entries[0].Categories.Count.Should().Be(1);
        }

        [Test]
        public void CanAddCorrectEntryForDeletedConstraint()
        {
            var diff = new DatabaseDiff();
            var tableDiff = new TableDiff();
            tableDiff.ConstraintsDeleted.Add(new Constraint("MyConstraint", "", ""));
            diff.TablesDiff.Add(tableDiff);

            var report = diff.GetDiffReport();

            DiffEntry entry;
            (entry = report.Categories[0].Entries[0].Categories[0].Entries.FirstOrDefault()).Should().NotBeNull();
            entry.Name.Should().Be("MyConstraint");
            entry.Type.Should().Be(DiffEntryType.Deleted);
        }

        [Test]
        public void CanAddCategoryToCategoryForChangedConstraint()
        {
            var diff = new DatabaseDiff();
            var tableDiff = new TableDiff();
            tableDiff.ConstraintsChanged.Add(new Constraint("", "", ""));
            diff.TablesDiff.Add(tableDiff);

            var report = diff.GetDiffReport();

            report.Categories[0].Entries.Count.Should().Be(1);
            report.Categories[0].Entries[0].Categories.Count.Should().Be(1);
        }

        [Test]
        public void CanAddCorrectEntryForChangedConstraint()
        {
            var diff = new DatabaseDiff();
            var tableDiff = new TableDiff();
            tableDiff.ConstraintsChanged.Add(new Constraint("MyConstraint", "", ""));
            diff.TablesDiff.Add(tableDiff);

            var report = diff.GetDiffReport();

            DiffEntry entry;
            (entry = report.Categories[0].Entries[0].Categories[0].Entries.FirstOrDefault()).Should().NotBeNull();
            entry.Name.Should().Be("MyConstraint");
            entry.Type.Should().Be(DiffEntryType.Changed);
        }

        [Test]
        public void CanAddCategoryWithAddedForeinKey()
        {
            var diff = new DatabaseDiff();
            diff.ForeignKeysAdded.Add(new ForeignKey("My_FK"));

            var report = diff.GetDiffReport();

            report.Categories.Count.Should().Be(1);
            report.Categories[0].Name.Should().Be("Foreign Keys");
        }

        [Test]
        public void CanAddCorrectEntryForAddedForeignKey()
        {
            var diff = new DatabaseDiff();
            diff.ForeignKeysAdded.Add(new ForeignKey("My_FK"));

            var report = diff.GetDiffReport();

            DiffEntry entry;
            (entry = report.Categories[0].Entries.FirstOrDefault()).Should().NotBeNull();
            entry.Name.Should().Be("My_FK");
            entry.Type.Should().Be(DiffEntryType.Added);
        }

        [Test]
        public void CanAddCategoryWithDeletedForeinKey()
        {
            var diff = new DatabaseDiff();
            diff.ForeignKeysDeleted.Add(new ForeignKey("My_FK"));

            var report = diff.GetDiffReport();

            report.Categories.Count.Should().Be(1);
        }

        [Test]
        public void CanAddCorrectEntryForDeletedForeignKey()
        {
            var diff = new DatabaseDiff();
            diff.ForeignKeysDeleted.Add(new ForeignKey("My_FK"));

            var report = diff.GetDiffReport();

            DiffEntry entry;
            (entry = report.Categories[0].Entries.FirstOrDefault()).Should().NotBeNull();
            entry.Name.Should().Be("My_FK");
            entry.Type.Should().Be(DiffEntryType.Deleted);
        }

        [Test]
        public void CanAddCategoryWithChangedForeinKey()
        {
            var diff = new DatabaseDiff();
            diff.ForeignKeysDiff.Add(new ForeignKey("My_FK"));

            var report = diff.GetDiffReport();

            report.Categories.Count.Should().Be(1);
        }

        [Test]
        public void CanAddCorrectEntryForChangedForeignKey()
        {
            var diff = new DatabaseDiff();
            diff.ForeignKeysDiff.Add(new ForeignKey("My_FK"));

            var report = diff.GetDiffReport();

            DiffEntry entry;
            (entry = report.Categories[0].Entries.FirstOrDefault()).Should().NotBeNull();
            entry.Name.Should().Be("My_FK");
            entry.Type.Should().Be(DiffEntryType.Changed);
        }

        [Test]
        public void CanAddCategoryWithAddedRoutine()
        {
            var diff = new DatabaseDiff();
            diff.RoutinesAdded.Add(new Routine("dbo", "MySP"));

            var report = diff.GetDiffReport();

            report.Categories.Count.Should().Be(1);
            report.Categories[0].Name.Should().Be("Routines");
        }

        [Test]
        public void CanAddCorrectEntryForAddedRoutine()
        {
            var diff = new DatabaseDiff();
            diff.RoutinesAdded.Add(new Routine("dbo", "My_Routine"));

            var report = diff.GetDiffReport();

            DiffEntry entry;
            (entry = report.Categories[0].Entries.FirstOrDefault()).Should().NotBeNull();
            entry.Name.Should().Be("My_Routine");
            entry.Type.Should().Be(DiffEntryType.Added);
        }

        [Test]
        public void CanAddCategoryWithDeletedRoutine()
        {
            var diff = new DatabaseDiff();
            diff.RoutinesDeleted.Add(new Routine("dbo", "MySP"));

            var report = diff.GetDiffReport();

            report.Categories.Count.Should().Be(1);
        }

        [Test]
        public void CanAddCorrectEntryForDeletedRoutine()
        {
            var diff = new DatabaseDiff();
            diff.RoutinesDeleted.Add(new Routine("dbo", "My_Routine"));

            var report = diff.GetDiffReport();

            DiffEntry entry;
            (entry = report.Categories[0].Entries.FirstOrDefault()).Should().NotBeNull();
            entry.Name.Should().Be("My_Routine");
            entry.Type.Should().Be(DiffEntryType.Deleted);
        }

        [Test]
        public void CanAddCategoryWithChangedRoutine()
        {
            var diff = new DatabaseDiff();
            diff.RoutinesDiff.Add(new Routine("dbo", "MySP"));

            var report = diff.GetDiffReport();

            report.Categories.Count.Should().Be(1);
        }

        [Test]
        public void CanAddCorrectEntryForChangedRoutine()
        {
            var diff = new DatabaseDiff();
            diff.RoutinesDiff.Add(new Routine("dbo","My_Routine"));

            var report = diff.GetDiffReport();

            DiffEntry entry;
            (entry = report.Categories[0].Entries.FirstOrDefault()).Should().NotBeNull();
            entry.Name.Should().Be("My_Routine");
            entry.Type.Should().Be(DiffEntryType.Changed);
        }

        [Test]
        public void CanAddCategoryWithChangedProperty()
        {
            var diff = new DatabaseDiff();
            diff.PropsChanged.Add(new DbProp("prop", "value"));

            var report = diff.GetDiffReport();

            report.Categories.Count.Should().Be(1);
            report.Categories[0].Name.Should().Be("Properties");
        }

        [Test]
        public void CanAddCorrectEntryForChangedProperty()
        {
            var diff = new DatabaseDiff();
            diff.PropsChanged.Add(new DbProp("prop", "value"));

            var report = diff.GetDiffReport();

            DiffEntry entry;
            (entry = report.Categories[0].Entries.FirstOrDefault()).Should().NotBeNull();
            entry.Name.Should().Be("prop");
            entry.Type.Should().Be(DiffEntryType.Changed);
        }
    }
}