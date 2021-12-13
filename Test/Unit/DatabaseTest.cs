using Microsoft.Extensions.Logging;
using SchemaZen.Library.Models;
using Xunit;
using Xunit.Abstractions;

namespace Test.Unit;

public class DatabaseTest {
	private readonly ILogger _logger;

	public DatabaseTest(ITestOutputHelper output) {
		_logger = output.BuildLogger();
	}

	[Fact]
	public void TestFindTableRegEx() {
		var db = CreateSampleDataForRegExTests();

		Assert.Equal(3, db.FindTablesRegEx("^cmic").Count);
		Assert.Single(db.FindTablesRegEx("Location"));
	}

	[Fact]
	public void TestFindTableRegEx_ExcludeOnly() {
		var db = CreateSampleDataForRegExTests();

		Assert.Equal(3, db.FindTablesRegEx(null, "^cmic").Count);
		Assert.Equal(5, db.FindTablesRegEx(null, "Location").Count);
	}

	[Fact]
	public void TestFindTableRegEx_BothIncludeExclude() {
		var db = CreateSampleDataForRegExTests();

		Assert.Equal(2, db.FindTablesRegEx("^cmic", "Code$").Count);
		Assert.Empty(db.FindTablesRegEx("Location", "Location"));
	}

	private static Database CreateSampleDataForRegExTests() {
		var db = new Database();
		db.Tables.Add(new Table("dbo", "cmicDeductible"));
		db.Tables.Add(new Table("dbo", "cmicZipCode"));
		db.Tables.Add(new Table("dbo", "cmicState"));
		db.Tables.Add(new Table("dbo", "Policy"));
		db.Tables.Add(new Table("dbo", "Location"));
		db.Tables.Add(new Table("dbo", "Rate"));
		return db;
	}

	[Fact]
	public void TestScriptDeletedProc() {
		var source = new Database();
		source.Routines.Add(new Routine("dbo", "test", null));
		source.FindRoutine("test", "dbo").RoutineType = Routine.RoutineKind.Procedure;
		source.FindRoutine("test", "dbo").Text = @"
create procedure [dbo].[test]
as 
select * from Table1
";

		var target = new Database();
		var scriptUp = target.Compare(source).Script();
		var scriptDown = source.Compare(target).Script();

		Assert.Contains(
			"drop procedure [dbo].[test]",
			scriptUp.ToLower());

		Assert.Contains(
			"create procedure [dbo].[test]",
			scriptDown.ToLower());
	}
}
