using System;
using System.Linq;
using NUnit.Framework;
using SchemaZen.Library.Models;

namespace SchemaZen.Tests {
	[TestFixture]
	public class FunctionTester {
		private const string _exampleFunc = @"
CREATE FUNCTION [dbo].udf_GetDate()
RETURNS DATETIME AS
BEGIN
	RETURN GETDATE()
END
";

		[Test]
		public void TestScript() {
			var f = new Routine("dbo", "udf_GetDate", null) {
				RoutineType = Routine.RoutineKind.Function,
				Text = _exampleFunc
			};
			Console.WriteLine(f.ScriptCreate());
			TestHelper.ExecBatchSql(f.ScriptCreate() + "\nGO", "");
			TestHelper.ExecSql("drop function [dbo].[udf_GetDate]", "");
		}

		[Test]
		public void TestScriptNoWarnings() {
			var f = new Routine("dbo", "udf_GetDate", null) {
				Text = _exampleFunc,
				RoutineType = Routine.RoutineKind.Function
			};
			Assert.IsFalse(f.Warnings().Any());
		}

		[Test]
		public void TestScriptWarnings() {
			var f = new Routine("dbo", "udf_GetDate2", null) {
				Text = _exampleFunc,
				RoutineType = Routine.RoutineKind.Function
			};
			Assert.IsTrue(f.Warnings().Any());
		}
	}
}
