using System.Data.SqlClient;
using SchemaZen.Library;
using SchemaZen.Tests;

namespace Test.Helpers;

// ensures a test db instance exists before running tests
public class TestDbFixture : IDisposable
{
    public TestDbFixture()
    {
        DBHelper.DropDb(ConfigHelper.TestDB);
        DBHelper.CreateDb(ConfigHelper.TestDB);
        SqlConnection.ClearAllPools();
    }

    public void Dispose()
    {
    }
}