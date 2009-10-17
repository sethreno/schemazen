using model;
using System;
using NUnit.Framework;

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
            TestHelper.ExecSql(f.ScriptCreate(), "");
            TestHelper.ExecSql("drop function [dbo].[udf_GetDate]", "");
        }

    }
}