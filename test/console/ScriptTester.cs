using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace test {

    [TestFixture()]
    class ScriptTester {

        [Test()]
        public void TestParse() {
            var cmd = new console.Script();
            string[] args = {"script", 
                                "cn:server=localhost;database=DEVDB;Trusted_Connection=yes;",
                                "d:\\DEVDB",
                                "-d",
                                "--data",
                                "^Lookup"};

            Assert.IsTrue(cmd.Parse(args));
        }
    }
}
