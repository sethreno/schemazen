using console;
using NUnit.Framework;

namespace test {
	[TestFixture]
	internal class DataArgTester {
		[Test]
		public void TestParse() {
			string[] args = {"--data", "Type$"};
			DataArg data = DataArg.Parse(args);
			Assert.IsNotNull(data);
			Assert.AreEqual("Type$", data.Value);

			string[] args2 = {"", "--dataTable1,Table2,Table3", ""};
			data = DataArg.Parse(args2);
			Assert.IsNotNull(data);
			Assert.AreEqual("Table1,Table2,Table3", data.Value);
		}
	}
}