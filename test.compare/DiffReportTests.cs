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
    }
}