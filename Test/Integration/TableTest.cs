using System.Text;
using Microsoft.Extensions.Logging;
using SchemaZen.Library.Models;
using Test.Integration.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Test.Integration;

[Trait("Category", "Integration")]
public class TableTest {
	private readonly TestDbHelper _dbHelper;

	private readonly ILogger _logger;

	public TableTest(ITestOutputHelper output, TestDbHelper dbHelper) {
		_logger = output.BuildLogger();
		_dbHelper = dbHelper;
	}

	[Fact]
	public async Task TestExportData() {
		var t = new Table("dbo", "Status");
		t.Columns.Add(new Column("id", "int", false, null));
		t.Columns.Add(new Column("code", "char", 1, false, null));
		t.Columns.Add(new Column("description", "varchar", 20, false, null));
		t.Columns.Find("id").Identity = new Identity(1, 1);
		t.AddConstraint(new Constraint("PK_Status", "PRIMARY KEY", "id"));

		await using var testDb = await _dbHelper.CreateTestDbAsync();
		await testDb.ExecSqlAsync(t.ScriptCreate());

		var dataIn =
			@"1	R	Ready
2	P	Processing
3	F	Frozen
";
		var filename = Path.GetTempFileName();

		var writer = File.AppendText(filename);
		writer.Write(dataIn);
		writer.Flush();
		writer.Close();

		t.ImportData(testDb.GetConnString(), filename);
		var sw = new StringWriter();
		t.ExportData(testDb.GetConnString(), sw);
		Assert.Equal(dataIn, sw.ToString());

		File.Delete(filename);
	}

	[Fact]
	public async Task TestImportAndExportIgnoringComputedData() {
		var t = new Table("dbo", "Status");
		t.Columns.Add(new Column("id", "int", false, null));
		t.Columns.Add(new Column("code", "char", 1, false, null));
		t.Columns.Add(new Column("description", "varchar", 20, false, null));
		var computedCol = new Column("computed", "varchar", false, null) {
			ComputedDefinition = "code + ' : ' + description"
		};
		t.Columns.Add(computedCol);
		t.Columns.Find("id").Identity = new Identity(1, 1);
		t.AddConstraint(new Constraint("PK_Status", "PRIMARY KEY", "id"));

		await using var testDb = await _dbHelper.CreateTestDbAsync();
		await testDb.ExecSqlAsync(t.ScriptCreate());

		var dataIn =
			@"1	R	Ready
2	P	Processing
3	F	Frozen
";
		var filename = Path.GetTempFileName();

		var writer = File.AppendText(filename);
		writer.Write(dataIn);
		writer.Flush();
		writer.Close();

		try {
			t.ImportData(testDb.GetConnString(), filename);
			var sw = new StringWriter();
			t.ExportData(testDb.GetConnString(), sw);
			Assert.Equal(dataIn, sw.ToString());
		} finally {
			File.Delete(filename);
		}
	}

	[Fact]
	public async Task TestImportAndExportDateTimeWithoutLosePrecision() {
		var t = new Table("dbo", "Dummy");
		t.Columns.Add(new Column("id", "int", false, null));
		t.Columns.Add(new Column("createdTime", "datetime", false, null));
		t.Columns.Find("id").Identity = new Identity(1, 1);
		t.AddConstraint(new Constraint("PK_Status", "PRIMARY KEY", "id"));

		await using var testDb = await _dbHelper.CreateTestDbAsync();
		await testDb.ExecSqlAsync(t.ScriptCreate());

		var dataIn =
			@"1	2017-02-21 11:20:30.1
2	2017-02-22 11:20:30.12
3	2017-02-23 11:20:30.123
";
		var filename = Path.GetTempFileName();

		var writer = File.AppendText(filename);
		writer.Write(dataIn);
		writer.Flush();
		writer.Close();

		try {
			t.ImportData(testDb.GetConnString(), filename);
			var sw = new StringWriter();
			t.ExportData(testDb.GetConnString(), sw);
			Assert.Equal(dataIn, sw.ToString());
		} finally {
			File.Delete(filename);
		}
	}

	[Fact]
	public async Task TestImportAndExportNonDefaultSchema() {
		var s = new Schema("example", "dbo");
		var t = new Table(s.Name, "Example");
		t.Columns.Add(new Column("id", "int", false, null));
		t.Columns.Add(new Column("code", "char", 1, false, null));
		t.Columns.Add(new Column("description", "varchar", 20, false, null));
		t.Columns.Find("id").Identity = new Identity(1, 1);
		t.AddConstraint(new Constraint("PK_Example", "PRIMARY KEY", "id"));

		await using var testDb = await _dbHelper.CreateTestDbAsync();
		await testDb.ExecSqlAsync(s.ScriptCreate());
		await testDb.ExecSqlAsync(t.ScriptCreate());

		var dataIn =
			@"1	R	Ready
2	P	Processing
3	F	Frozen
";
		var filename = Path.GetTempFileName();

		var writer = File.AppendText(filename);
		writer.Write(dataIn);
		writer.Flush();
		writer.Close();

		try {
			t.ImportData(testDb.GetConnString(), filename);
			var sw = new StringWriter();
			t.ExportData(testDb.GetConnString(), sw);
			Assert.Equal(dataIn, sw.ToString());
		} finally {
			File.Delete(filename);
		}
	}

	[Fact]
	public async Task TestLargeAmountOfRowsImportAndExport() {
		var t = new Table("dbo", "TestData");
		t.Columns.Add(new Column("test_field", "int", false, null));
		t.AddConstraint(
			new Constraint("PK_TestData", "PRIMARY KEY", "test_field") {
				IndexType = "NONCLUSTERED"
			});
		t.AddConstraint(
			new Constraint("IX_TestData_PK", "INDEX", "test_field") {
				// clustered index is required to ensure the row order is the same as what we import
				IndexType = "CLUSTERED",
				Table = t,
				Unique = true
			});

		await using var testDb = await _dbHelper.CreateTestDbAsync();
		await testDb.ExecSqlAsync(t.ScriptCreate());

		var filename = Path.GetTempFileName();

		var writer = File.CreateText(filename);
		var sb = new StringBuilder();

		for (var i = 0; i < Table.RowsInBatch * 4.2; i++) {
			sb.AppendLine(i.ToString());
			writer.WriteLine(i.ToString());
		}

		writer.Flush();
		writer.Close();

		var dataIn = sb.ToString();
		Assert.Equal(
			dataIn,
			File.ReadAllText(
				filename)); // just prove that the file and the string are the same, to make the next assertion meaningful!

		try {
			t.ImportData(testDb.GetConnString(), filename);
			var sw = new StringWriter();
			t.ExportData(testDb.GetConnString(), sw);

			Assert.Equal(dataIn, sw.ToString());
		} finally {
			File.Delete(filename);
		}
	}

	[Fact]
	public async Task TestScript() {
		//create a table with all known types, script it, and execute the script
		var t = new Table("dbo", "AllTypesTest");
		t.Columns.Add(new Column("a", "bigint", false, null));
		t.Columns.Add(new Column("b", "binary", 50, false, null));
		t.Columns.Add(new Column("c", "bit", false, null));
		t.Columns.Add(new Column("d", "char", 10, false, null));
		t.Columns.Add(new Column("e", "datetime", false, null));
		t.Columns.Add(new Column("f", "decimal", 18, 0, false, null));
		t.Columns.Add(new Column("g", "float", false, null));
		t.Columns.Add(new Column("h", "image", false, null));
		t.Columns.Add(new Column("i", "int", false, null));
		t.Columns.Add(new Column("j", "money", false, null));
		t.Columns.Add(new Column("k", "nchar", 10, false, null));
		t.Columns.Add(new Column("l", "ntext", false, null));
		t.Columns.Add(new Column("m", "numeric", 18, 0, false, null));
		t.Columns.Add(new Column("n", "nvarchar", 50, false, null));
		t.Columns.Add(new Column("o", "nvarchar", -1, false, null));
		t.Columns.Add(new Column("p", "real", false, null));
		t.Columns.Add(new Column("q", "smalldatetime", false, null));
		t.Columns.Add(new Column("r", "smallint", false, null));
		t.Columns.Add(new Column("s", "smallmoney", false, null));
		t.Columns.Add(new Column("t", "sql_variant", false, null));
		t.Columns.Add(new Column("u", "text", false, null));
		t.Columns.Add(new Column("v", "timestamp", false, null));
		t.Columns.Add(new Column("w", "tinyint", false, null));
		t.Columns.Add(new Column("x", "uniqueidentifier", false, null));
		t.Columns.Add(new Column("y", "varbinary", 50, false, null));
		t.Columns.Add(new Column("z", "varbinary", -1, false, null));
		t.Columns.Add(
			new Column(
				"aa",
				"varchar",
				50,
				true,
				new Default("DF_AllTypesTest_aa", "'asdf'", false)));
		t.Columns.Add(new Column("bb", "varchar", -1, true, null));
		t.Columns.Add(new Column("cc", "xml", true, null));
		t.Columns.Add(new Column("dd", "hierarchyid", false, null));

		await using var testDb = await _dbHelper.CreateTestDbAsync();
		await testDb.ExecSqlAsync(t.ScriptCreate());
	}
}
