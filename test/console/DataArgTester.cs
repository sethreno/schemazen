using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace test {
    [TestFixture()]
    class DataArgTester {

        [Test()]
        public void TestParse() {
            string[] args = { "--data", "Type$" };
            var data = console.DataArg.Parse(args);
            Assert.IsNotNull(data);
            Assert.AreEqual("Type$", data.Value);

            string[] args2 = {"", "--dataTable1,Table2,Table3", ""};
            data = console.DataArg.Parse(args2);
            Assert.IsNotNull(data);
            Assert.AreEqual("Table1,Table2,Table3", data.Value);
        }
    }
}
