using System;
using System.Linq;
using NUnit.Framework;
using SchemaZen.model;

namespace SchemaZen.test {
	[TestFixture]
	public class FunctionTester {
		private const string ExampleFunc = @"
CREATE FUNCTION [dbo].udf_GetDate()
RETURNS DATETIME AS
BEGIN
	RETURN GETDATE()
END
";

		[Test]
		public void TestScript() {
			var f = new Routine("dbo", "udf_GetDate") {
														  RoutineType = Routine.RoutineKind.Function,
														  Text = ExampleFunc
													  };
			Console.WriteLine(f.ScriptCreate(null));
			TestHelper.ExecBatchSql(f.ScriptCreate(null) + "\nGO", "");
			TestHelper.ExecSql("drop function [dbo].[udf_GetDate]", "");
		}

		[Test]
		public void TestScriptNoWarnings()
		{
			var f = new Routine("dbo", "udf_GetDate") {
														  Text = ExampleFunc,
														  RoutineType = Routine.RoutineKind.Function
													  };
			Assert.IsFalse(f.Warnings().Any());
		}

		[Test]
		public void TestScriptWarnings()
		{
			var f = new Routine("dbo", "udf_GetDate2") {
														   Text = ExampleFunc,
														   RoutineType = Routine.RoutineKind.Function
													   };
			Assert.IsTrue(f.Warnings().Any());
		}
	}
}
