using SchemaZen.Library;
using Xunit;

namespace Test.Unit;

public class BatchSqlParserTest {
	[Fact]
	public void CanParseCommentBeforeGoStatement() {
		const string script = @"SELECT FOO
/*TEST*/ GO
BAR";
		var scripts = BatchSqlParser.SplitBatch(script);
		Assert.Equal(2, scripts.Length);
	}

	[Fact]
	public void CanParseCommentWithQuoteChar() {
		const string script =
			@"/* Add the Url column to the subtext_Log table if it doesn't exist */
	ADD [Url] VARCHAR(255) NULL
GO
			AND             COLUMN_NAME = 'BlogGroup') IS NULL";
		var scripts = BatchSqlParser.SplitBatch(script);
		Assert.Equal(2, scripts.Length);
	}

	[Fact]
	public void CanParseDashDashCommentWithQuoteChar() {
		const string script =
			@"-- Add the Url column to the subtext_Log table if it doesn't exist
SELECT * FROM BLAH
GO
PRINT 'FOO'";
		var scripts = BatchSqlParser.SplitBatch(script);
		Assert.Equal(2, scripts.Length);
	}

	[Fact]
	public void CanParseGoWithDashDashCommentAfter() {
		const string script = @"SELECT * FROM foo;
GO --  Hello Phil
CREATE PROCEDURE dbo.Test AS SELECT * FROM foo";
		var scripts = BatchSqlParser.SplitBatch(script);
		Assert.Equal(2, scripts.Length);
	}

	[Fact]
	public void CanParseLineEndingInDashDashComment() {
		const string script = @"SELECT * FROM BLAH -- Comment
GO
FOOBAR
GO";
		var scripts = BatchSqlParser.SplitBatch(script);
		Assert.Equal(2, scripts.Length);
	}

	[Fact]
	public void CanParseNestedComments() {
		const string script = @"/*
select 1
/* nested comment */
go
delete from users
-- */";
		var scripts = BatchSqlParser.SplitBatch(script);
		Assert.Single(scripts);
	}

	[Fact]
	public void CanParseQuotedCorrectly() {
		const string script = @"INSERT INTO #Indexes
	EXEC sp_helpindex 'dbo.subtext_URLs'";

		var scripts = BatchSqlParser.SplitBatch(script);
		Assert.Equal(script, scripts[0]);
	}

	[Fact]
	public void CanParseSimpleScript() {
		var script = "Test" + Environment.NewLine + "go";
		var scripts = BatchSqlParser.SplitBatch(script);
		Assert.Single(scripts);
		Assert.Equal("Test" + Environment.NewLine, scripts[0]);
	}

	[Fact]
	public void CanParseSimpleScriptEndingInNewLine() {
		var script = "Test" + Environment.NewLine + "GO" + Environment.NewLine;
		var scripts = BatchSqlParser.SplitBatch(script);
		Assert.Single(scripts);
		Assert.Equal("Test" + Environment.NewLine, scripts[0]);
	}

	[Fact]
	public void CanParseSuccessiveGoStatements() {
		const string script = @"GO
GO";
		var scripts = BatchSqlParser.SplitBatch(script);
		Assert.Empty(scripts);
	}

	[Fact]
	public void MultiLineQuoteShouldNotBeSplitByGoKeyword() {
		var script = "PRINT '" + Environment.NewLine
			+ "GO" + Environment.NewLine
			+ "SELECT * FROM BLAH" + Environment.NewLine
			+ "GO" + Environment.NewLine
			+ "'";

		var scripts = BatchSqlParser.SplitBatch(script);

		Assert.Equal(script, scripts[0]);
		Assert.Single(scripts);
	}

	[Fact]
	public void MultiLineQuoteShouldNotIgnoreDoubleQuote() {
		var script = "PRINT '" + Environment.NewLine
			+ "''" + Environment.NewLine
			+ "GO" + Environment.NewLine
			+ "/*" + Environment.NewLine
			+ "GO"
			+ "'";

		var scripts = BatchSqlParser.SplitBatch(script);

		Assert.Single(scripts);
		Assert.Equal(script, scripts[0]);
	}

	/// <summary>
	///     Makes sure that ParseScript parses correctly.
	/// </summary>
	[Fact]
	public void ParseScriptParsesCorrectly() {
		const string script = @"SET QUOTED_IDENTIFIER OFF
-- Comment
Go
			
SET ANSI_NULLS ON


GO

GO


SET ANSI_NULLS ON


CREATE TABLE [<username,varchar,dbo>].[blog_Gost] (
	[HostUserName] [nvarchar] (64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[Password] [nvarchar] (64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[Salt] [nvarchar] (32) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[DateCreated] [datetime] NOT NULL
) ON [PRIMARY]
gO

";
		var scripts = BatchSqlParser.SplitBatch(script);
		Assert.Equal(3, scripts.Length);
	}

	[Fact]
	public void SemiColonDoesNotSplitScript() {
		const string script = "CREATE PROC Blah AS SELECT FOO; SELECT Bar;";
		var scripts = BatchSqlParser.SplitBatch(script);
		Assert.Single(scripts);
	}

	[Fact]
	public void SlashStarCommentAfterGoThrowsException() {
		const string script = @"PRINT 'blah'
GO /* blah */";

		Assert.Equal(2, BatchSqlParser.SplitBatch(script).Length);
		//why should this throw an exception?
	}

	[Fact]
	public void TestCommentFollowingGO() {
		var scripts = BatchSqlParser.SplitBatch("/*script1*/GO/*script2*/");
		Assert.Equal(2, scripts.Length);
		Assert.Equal("/*script1*/", scripts[0]);
		Assert.Equal("/*script2*/", scripts[1]);
	}

	[Fact]
	public void TestCommentPrecedingGO() {
		var scripts = BatchSqlParser.SplitBatch("/*script1*/GO--script2");
		Assert.Equal(2, scripts.Length);
		Assert.Equal("/*script1*/", scripts[0]);
		Assert.Equal("--script2", scripts[1]);
	}

	[Fact]
	public void TestLeadingTablsDashDash() {
		string[] scripts;
		scripts = BatchSqlParser.SplitBatch("\t\t\t\t-- comment");
		Assert.Equal("\t\t\t\t-- comment", scripts[0]);
	}

	[Fact]
	public void TestLeadingTabsParameter() {
		string[] scripts;
		scripts = BatchSqlParser.SplitBatch("\t\t\t\t@param");
		Assert.Equal("\t\t\t\t@param", scripts[0]);
	}

	[Fact]
	public void TestLeadingTabsSingleQuotes() {
		string[] scripts;
		scripts = BatchSqlParser.SplitBatch("\t\t\t\t'AddProjectToSourceSafe',");
		Assert.Equal("\t\t\t\t'AddProjectToSourceSafe',", scripts[0]);
	}

	[Fact]
	public void TestLeadingTabsSlashStar() {
		string[] scripts;
		scripts = BatchSqlParser.SplitBatch("\t\t\t\t/* comment */");
		Assert.Equal("\t\t\t\t/* comment */", scripts[0]);
	}

	[Fact]
	public void TestScriptWithGOTO() {
		var script = @"script 1
GO
script 2
GOTO <-- not a GO <-- niether is this
NOGO <-- also not a GO <-- still no
";
		var scripts = BatchSqlParser.SplitBatch(script);
		Assert.Equal(2, scripts.Length);
	}

	[Fact]
	public void TestSplitGOInComment() {
		string[] scripts;
		scripts = BatchSqlParser.SplitBatch(
			@"
1:1
-- GO ride a bike
1:2
");
		//shoud be 1 script
		Assert.Single(scripts);
	}

	[Fact]
	public void TestSplitGOInQuotes() {
		string[] scripts;
		scripts = BatchSqlParser.SplitBatch(
			@"
1:1 ' 
GO
' 1:2
");
		//should be 1 script
		Assert.Single(scripts);
	}

	[Fact]
	public void TestSplitGONoEndLine() {
		string[] scripts;
		scripts = BatchSqlParser.SplitBatch(
			@"
1:1
1:2
GO");
		//should be 1 script with no 'GO'
		Assert.Single(scripts);
		Assert.DoesNotContain("GO", scripts[0]);
	}

	[Fact]
	public void TestSplitMultipleGOs() {
		string[] scripts;
		scripts = BatchSqlParser.SplitBatch(
			@"
1:1
GO
GO
GO
GO
2:1
");
		//should be 2 scripts
		Assert.Equal(2, scripts.Length);
	}

	[Fact]
	public void IgnoresSlashStarInsideQuotedIdentifier() {
		var scripts = BatchSqlParser.SplitBatch(
			@"
select 1 as ""/*""
GO
SET ANSI_NULLS OFF
GO
");
		//should be 2 scripts
		Assert.Equal(2, scripts.Length);
	}

	[Fact]
	public void IgnoresGoInsideBrackets() {
		var scripts = BatchSqlParser.SplitBatch(
			@"
1:1
select 1 as [GO]
SET ANSI_NULLS OFF
GO
");
		Assert.Single(scripts);
	}

	[Fact]
	public void IgnoresGoInsideQuotedIdentifier() {
		var scripts = BatchSqlParser.SplitBatch(
			@"
1:1
select 1 as ""GO""
SET ANSI_NULLS OFF
GO
");
		Assert.Single(scripts);
	}
}
