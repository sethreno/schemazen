using System.Data.SqlClient;
using SchemaZen.Library;
using SchemaZen.Tests;
using Xunit;

namespace Test.Helpers;

// ensures a test db instance exists before running tests
public class TestDbFixture : IDisposable {
	public TestDbFixture() {
		DBHelper.DropDb(ConfigHelper.TestDB);
		DBHelper.CreateDb(ConfigHelper.TestDB);
		SqlConnection.ClearAllPools();
	}

	public void Dispose() { }
}

[CollectionDefinition("TestDb")]
public class TestDbCollection : ICollectionFixture<TestDbFixture> {
	// This class has no code, and is never created. Its purpose is simply
	// to be the place to apply [CollectionDefinition] and all the
	// ICollectionFixture<> interfaces.
}
