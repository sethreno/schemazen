using Microsoft.Extensions.Logging;
using SchemaZen.Library.Models;
using Xunit;
using Xunit.Abstractions;

namespace Test.Unit;

public class TableTest {
	private readonly ILogger _logger;

	public TableTest(ITestOutputHelper output) {
		_logger = output.BuildLogger();
	}

	[Fact]
	public void CompareConstraints() {
		var t1 = new Table("dbo", "Test");
		var t2 = new Table("dbo", "Test");
		var diff = default(TableDiff);

		//test equal
		t1.Columns.Add(new Column("first", "varchar", 30, false, null));
		t2.Columns.Add(new Column("first", "varchar", 30, false, null));
		t1.AddConstraint(
			Constraint.CreateCheckedConstraint("IsTomorrow", true, false, "fnTomorrow()"));
		t2.AddConstraint(
			Constraint.CreateCheckedConstraint("IsTomorrow", false, false, "Tomorrow <> 1"));

		diff = t1.Compare(t2);
		Assert.Single(diff.ConstraintsChanged);
		Assert.NotNull(diff);
		Assert.True(diff.IsDiff);
	}

	[Fact]
	public void TestCompare() {
		var t1 = new Table("dbo", "Test");
		var t2 = new Table("dbo", "Test");
		var diff = default(TableDiff);

		//test equal
		t1.Columns.Add(new Column("first", "varchar", 30, false, null));
		t2.Columns.Add(new Column("first", "varchar", 30, false, null));
		t1.AddConstraint(new Constraint("PK_Test", "PRIMARY KEY", "first"));
		t2.AddConstraint(new Constraint("PK_Test", "PRIMARY KEY", "first"));

		diff = t1.Compare(t2);
		Assert.NotNull(diff);
		Assert.False(diff.IsDiff);

		//test add
		t1.Columns.Add(new Column("second", "varchar", 30, false, null));
		diff = t1.Compare(t2);
		Assert.True(diff.IsDiff);
		Assert.Single(diff.ColumnsAdded);

		//test delete
		diff = t2.Compare(t1);
		Assert.True(diff.IsDiff);
		Assert.Single(diff.ColumnsDropped);

		//test diff
		t1.Columns.Items[0].Length = 20;
		diff = t1.Compare(t2);
		Assert.True(diff.IsDiff);
		Assert.Single(diff.ColumnsDiff);

		_logger.LogTrace("--- create ----");
		_logger.LogTrace(t1.ScriptCreate());

		_logger.LogTrace("--- migrate up ---");
		_logger.LogTrace(t1.Compare(t2).Script());

		_logger.LogTrace("--- migrate down ---");
		_logger.LogTrace(t2.Compare(t1).Script());
	}

	[Fact]
	public void TestScriptNonSupportedColumn() {
		Assert.Throws<NotSupportedException>(() => {
			var t = new Table("dbo", "bla");
			t.Columns.Add(new Column("a", "madeuptype", true, null));
			t.ScriptCreate();
		});
	}
}
