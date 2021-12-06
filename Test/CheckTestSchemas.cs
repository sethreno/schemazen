using SchemaZen.Library.Models;
using Xunit;

namespace SchemaZen.Tests;

[Collection("CheckTestSchemas")]
public class CheckTestSchemas {
	[Fact]
	[Trait("Category", "Integration")]
	public void TestCopyTestSchemas() {
		// Regression tests databases scripted by other tools.
		// To add a new test script the entire database to a single file and
		// put it in the test_schemas directory.
		var files = Directory.GetFiles(ConfigHelper.TestSchemaDir).ToList();
		for (var i = 0; i < files.Count; i++) {
			var script = files[i];
			Console.WriteLine("Testing {0}", script);
			TestCopySchema(script, $"{i}");
		}
	}

	private static void TestCopySchema(
		string pathToSchemaScript,
		string dbSuffix) {
		var sourceDbName = $"CopySchemaSource_{dbSuffix}";
		var destDbName = $"CopySchemaDest_{dbSuffix}";

		TestHelper.DropDb(sourceDbName, "master");
		TestHelper.DropDb(destDbName, "master");

		//create the db from sql script
		TestHelper.ExecSql($"CREATE DATABASE {sourceDbName}", "master");
		TestHelper.ExecBatchSql(File.ReadAllText(pathToSchemaScript), sourceDbName);

		//load the model from newly created db and create a copy
		var copy = new Database(destDbName);
		copy.Connection = TestHelper.GetConnString(sourceDbName);
		copy.Load();
		var scripted = copy.ScriptCreate();
		TestHelper.ExecBatchSql(scripted, "master");

		//compare the dbs to make sure they are the same
		var source = new Database(sourceDbName) {
			Connection = TestHelper.GetConnString(sourceDbName)
		};
		source.Load();
		copy.Load();
		Assert.False(source.Compare(copy).IsDiff);
	}
}
