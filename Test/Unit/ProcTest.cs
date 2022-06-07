using Microsoft.Extensions.Logging;
using SchemaZen.Library.Models;
using Xunit;
using Xunit.Abstractions;

namespace Test.Unit;

public class ProcTest {
	private readonly ILogger _logger;

	public ProcTest(ITestOutputHelper output) {
		_logger = output.BuildLogger();
	}

	[Fact]
	public void TestScriptWarnings() {
		const string baseText = @"--example of routine that has been renamed since creation
CREATE PROCEDURE {0}
	@id int
AS
	select * from Address where id = @id
";
		var getAddress = new Routine("dbo", "GetAddress", null);
		getAddress.RoutineType = Routine.RoutineKind.Procedure;

		getAddress.Text = string.Format(baseText, "[dbo].[NamedDifferently]");
		Assert.True(getAddress.Warnings().Any());
		getAddress.Text = string.Format(baseText, "dbo.NamedDifferently");
		Assert.True(getAddress.Warnings().Any());

		getAddress.Text = string.Format(baseText, "dbo.[GetAddress]");
		Assert.False(getAddress.Warnings().Any());

		getAddress.Text = string.Format(baseText, "dbo.GetAddress");
		Assert.False(getAddress.Warnings().Any());

		getAddress.Text = string.Format(baseText, "GetAddress");
		Assert.False(getAddress.Warnings().Any());
	}
}
