using Microsoft.Extensions.Logging;
using SchemaZen.Library.Models;
using Test.Integration.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Test.Integration;

[Trait("Category", "Integration")]
public class ForeignKeyTest {
	private readonly TestDbHelper _dbHelper;

	private readonly ILogger _logger;

	public ForeignKeyTest(ITestOutputHelper output, TestDbHelper dbHelper) {
		_logger = output.BuildLogger();
		_dbHelper = dbHelper;
	}

	[Fact]
	public async Task TestMultiColumnKey() {
		var t1 = new Table("dbo", "t1");
		t1.Columns.Add(new Column("c2", "varchar", 10, false, null));
		t1.Columns.Add(new Column("c1", "int", false, null));
		t1.AddConstraint(new Constraint("pk_t1", "PRIMARY KEY", "c1,c2"));

		var t2 = new Table("dbo", "t2");
		t2.Columns.Add(new Column("c1", "int", false, null));
		t2.Columns.Add(new Column("c2", "varchar", 10, false, null));
		t2.Columns.Add(new Column("c3", "int", false, null));

		var fk = new ForeignKey(t2, "fk_test", "c3,c2", t1, "c1,c2");

		await using var testDb = await _dbHelper.CreateTestDbAsync();
		await testDb.ExecSqlAsync(t1.ScriptCreate());
		await testDb.ExecSqlAsync(t2.ScriptCreate());
		await testDb.ExecSqlAsync(fk.ScriptCreate());

		var db = new Database("TESTDB");
		db.Connection = testDb.GetConnString();
		db.Load();

		Assert.Equal("c3", db.FindForeignKey("fk_test", "dbo").Columns[0]);
		Assert.Equal("c2", db.FindForeignKey("fk_test", "dbo").Columns[1]);
		Assert.Equal("c1", db.FindForeignKey("fk_test", "dbo").RefColumns[0]);
		Assert.Equal("c2", db.FindForeignKey("fk_test", "dbo").RefColumns[1]);
	}

	[Fact]
	public async Task TestScript() {
		var person = new Table("dbo", "Person");
		person.Columns.Add(new Column("id", "int", false, null));
		person.Columns.Add(new Column("name", "varchar", 50, false, null));
		person.Columns.Find("id").Identity = new Identity(1, 1);
		person.AddConstraint(new Constraint("PK_Person", "PRIMARY KEY", "id"));

		var address = new Table("dbo", "Address");
		address.Columns.Add(new Column("id", "int", false, null));
		address.Columns.Add(new Column("personId", "int", false, null));
		address.Columns.Add(new Column("street", "varchar", 50, false, null));
		address.Columns.Add(new Column("city", "varchar", 50, false, null));
		address.Columns.Add(new Column("state", "char", 2, false, null));
		address.Columns.Add(new Column("zip", "varchar", 5, false, null));
		address.Columns.Find("id").Identity = new Identity(1, 1);
		address.AddConstraint(new Constraint("PK_Address", "PRIMARY KEY", "id"));

		var fk = new ForeignKey(
			address, "FK_Address_Person", "personId", person, "id", "", "CASCADE");

		await using var testDb = await _dbHelper.CreateTestDbAsync();
		await testDb.ExecSqlAsync(person.ScriptCreate());
		await testDb.ExecSqlAsync(address.ScriptCreate());
		await testDb.ExecSqlAsync(fk.ScriptCreate());

		var db = new Database("TESTDB");
		db.Connection = testDb.GetConnString();
		db.Load();

		Assert.NotNull(db.FindTable("Person", "dbo"));
		Assert.NotNull(db.FindTable("Address", "dbo"));
		Assert.NotNull(db.FindForeignKey("FK_Address_Person", "dbo"));
	}

	[Fact]
	public async Task TestScriptForeignKeyWithNoName() {
		var t1 = new Table("dbo", "t1");
		t1.Columns.Add(new Column("c2", "varchar", 10, false, null));
		t1.Columns.Add(new Column("c1", "int", false, null));
		t1.AddConstraint(new Constraint("pk_t1", "PRIMARY KEY", "c1,c2"));

		var t2 = new Table("dbo", "t2");
		t2.Columns.Add(new Column("c1", "int", false, null));
		t2.Columns.Add(new Column("c2", "varchar", 10, false, null));
		t2.Columns.Add(new Column("c3", "int", false, null));

		var fk = new ForeignKey(t2, "fk_ABCDEF", "c3,c2", t1, "c1,c2");
		fk.IsSystemNamed = true;

		await using var testDb = await _dbHelper.CreateTestDbAsync();
		await testDb.ExecSqlAsync(t1.ScriptCreate());
		await testDb.ExecSqlAsync(t2.ScriptCreate());
		await testDb.ExecSqlAsync(fk.ScriptCreate());

		var db = new Database("TESTDB");
		db.Connection = testDb.GetConnString();
		db.Load();

		Assert.Single(db.ForeignKeys);

		var fkCopy = db.ForeignKeys.Single();
		Assert.Equal("c3", fkCopy.Columns[0]);
		Assert.Equal("c2", fkCopy.Columns[1]);
		Assert.Equal("c1", fkCopy.RefColumns[0]);
		Assert.Equal("c2", fkCopy.RefColumns[1]);
		Assert.True(fkCopy.IsSystemNamed);
	}
}
