using System;
using NUnit.Framework;
using model;

namespace test{
    [TestFixture()]
    public class FunctionTester {

        [Test()]
        public void TestScript() {
            Routine f = new Routine("dbo", "udf_GetDate");
            f.Text = @"
CREATE FUNCTION [dbo].[udf_GetDate]()
RETURNS DATETIME AS
BEGIN
    RETURN GETDATE()
END
";
            Console.WriteLine(f.ScriptCreate());
            TestHelper.ExecBatchSql(f.ScriptCreate() + "\nGO" , "");
            TestHelper.ExecSql("drop function [dbo].[udf_GetDate]", "");
        }

    }
}