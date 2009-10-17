using model;
using System;
using NUnit.Framework;

namespace test {
    [TestFixture()]
    public class DBHelperTester {

        [Test()]
        public void TestSplitGONoEndLine() {
            string[] scripts = null;
            scripts = DBHelper.SplitBatchSql(@"
1:1
1:2
GO");
            //should be 1 script with no 'GO'
            Assert.AreEqual(1, scripts.Length);
            Assert.IsFalse(scripts[0].Contains("GO"));
        }

        [Test()]
        public void TestSplitGOInComment() {
            string[] scripts = null;
            scripts = DBHelper.SplitBatchSql(@"
1:1
-- GO eff yourself
1:2
");
            //shoud be 1 script
            Assert.AreEqual(1, scripts.Length);
        }

        [Test()]
        public void TestSplitGOInQuotes() {
            string[] scripts = null;
            scripts = DBHelper.SplitBatchSql(@"
1:1 ' 
GO
' 1:2
");
            //should be 1 script
            Assert.AreEqual(1, scripts.Length);
        }

        [Test()]
        public void TestSplitMultipleGOs() {
            string[] scripts = null;
            scripts = DBHelper.SplitBatchSql(@"
1:1
GO
GO
GO
GO
2:1
");
            //should be 2 scripts
            Assert.AreEqual(2, scripts.Length);
        }
    }
}