using System;
using NUnit.Framework;
using SchemaZen.Library;

namespace SchemaZen.Tests {
	[TestFixture]
	public class BatchSqlParserTester {
		[Test]
		public void CanParseCommentBeforeGoStatement() {
			const string script = @"SELECT FOO
/*TEST*/ GO
BAR";
			var scripts = BatchSqlParser.SplitBatch(script);
			Assert.AreEqual(2, scripts.Length);
		}

		[Test]
		public void CanParseCommentWithQuoteChar() {
			const string script = @"/* Add the Url column to the subtext_Log table if it doesn't exist */
		ADD [Url] VARCHAR(255) NULL
GO
				AND             COLUMN_NAME = 'BlogGroup') IS NULL";
			var scripts = BatchSqlParser.SplitBatch(script);
			Assert.AreEqual(2, scripts.Length);
		}

		[Test]
		public void CanParseDashDashCommentWithQuoteChar() {
			const string script = @"-- Add the Url column to the subtext_Log table if it doesn't exist
SELECT * FROM BLAH
GO
PRINT 'FOO'";
			var scripts = BatchSqlParser.SplitBatch(script);
			Assert.AreEqual(2, scripts.Length);
		}

		[Test]
		public void CanParseGoWithDashDashCommentAfter() {
			const string script = @"SELECT * FROM foo;
 GO --  Hello Phil
CREATE PROCEDURE dbo.Test AS SELECT * FROM foo";
			var scripts = BatchSqlParser.SplitBatch(script);
			Assert.AreEqual(2, scripts.Length);
		}

		[Test]
		public void CanParseLineEndingInDashDashComment() {
			const string script = @"SELECT * FROM BLAH -- Comment
GO
FOOBAR
GO";
			var scripts = BatchSqlParser.SplitBatch(script);
			Assert.AreEqual(2, scripts.Length);
		}

		[Test]
		public void CanParseNestedComments() {
			const string script = @"/*
select 1
/* nested comment */
go
delete from users
-- */";
			var scripts = BatchSqlParser.SplitBatch(script);
			Assert.AreEqual(1, scripts.Length, "This contains a comment and no scripts.");
		}

		[Test]
		public void CanParseQuotedCorrectly() {
			const string script = @"INSERT INTO #Indexes
		EXEC sp_helpindex 'dbo.subtext_URLs'";

			var scripts = BatchSqlParser.SplitBatch(script);
			Assert.AreEqual(script, scripts[0], "Script text should not be modified");
		}

		[Test]
		public void CanParseSimpleScript() {
			var script = "Test" + Environment.NewLine + "go";
			var scripts = BatchSqlParser.SplitBatch(script);
			Assert.AreEqual(1, scripts.Length);
			Assert.AreEqual("Test" + Environment.NewLine, scripts[0]);
		}

		[Test]
		public void CanParseSimpleScriptEndingInNewLine() {
			var script = "Test" + Environment.NewLine + "GO" + Environment.NewLine;
			var scripts = BatchSqlParser.SplitBatch(script);
			Assert.AreEqual(1, scripts.Length);
			Assert.AreEqual("Test" + Environment.NewLine, scripts[0]);
		}

		[Test]
		public void CanParseSuccessiveGoStatements() {
			const string script = @"GO
GO";
			var scripts = BatchSqlParser.SplitBatch(script);
			Assert.AreEqual(0, scripts.Length, "Expected no scripts since they would be empty.");
		}

		[Test]
		public void MultiLineQuoteShouldNotBeSplitByGoKeyword() {
			var script = "PRINT '" + Environment.NewLine
						 + "GO" + Environment.NewLine
						 + "SELECT * FROM BLAH" + Environment.NewLine
						 + "GO" + Environment.NewLine
						 + "'";

			var scripts = BatchSqlParser.SplitBatch(script);

			Assert.AreEqual(script, scripts[0]);
			Assert.AreEqual(1, scripts.Length, "expected only one script");
		}

		[Test]
		public void MultiLineQuoteShouldNotIgnoreDoubleQuote() {
			var script = "PRINT '" + Environment.NewLine
						 + "''" + Environment.NewLine
						 + "GO" + Environment.NewLine
						 + "/*" + Environment.NewLine
						 + "GO"
						 + "'";

			var scripts = BatchSqlParser.SplitBatch(script);

			Assert.AreEqual(1, scripts.Length);
			Assert.AreEqual(script, scripts[0]);
		}

		/// <summary>
		///     Makes sure that ParseScript parses correctly.
		/// </summary>
		[Test]
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
			Assert.AreEqual(3, scripts.Length, "This should parse to three scripts.");
			//for (int i = 0; i < scripts.Length; i++) {
			//    Script sqlScript = scripts[i];
			//    Assert.IsFalse(sqlScript.ScriptText.StartsWith("GO"), "Script '" + i + "' failed had a GO statement");
			//}

			//string expectedThirdScriptBeginning = "SET ANSI_NULLS ON "
			//                                      + Environment.NewLine
			//                                      + Environment.NewLine
			//                                      + Environment.NewLine +
			//                                      "CREATE TABLE [<username,varchar,dbo>].[blog_Gost]";

			//Assert.AreEqual(expectedThirdScriptBeginning,
			//                scripts[2].OriginalScriptText.Substring(0, expectedThirdScriptBeginning.Length),
			//                "Script not parsed correctly");

			//scripts.TemplateParameters.SetValue("username", "haacked");

			//expectedThirdScriptBeginning = "SET ANSI_NULLS ON "
			//                               + Environment.NewLine
			//                               + Environment.NewLine
			//                               + Environment.NewLine + "CREATE TABLE [haacked].[blog_Gost]";

			//Assert.AreEqual(expectedThirdScriptBeginning,
			//                scripts[2].ScriptText.Substring(0, expectedThirdScriptBeginning.Length),
			//                "Script not parsed correctly");
		}

		[Test]
		public void SemiColonDoesNotSplitScript() {
			const string script = "CREATE PROC Blah AS SELECT FOO; SELECT Bar;";
			var scripts = BatchSqlParser.SplitBatch(script);
			Assert.AreEqual(1, scripts.Length, "Expected no scripts since they would be empty.");
		}

		[Test]
		public void SlashStarCommentAfterGoThrowsException() {
			const string script = @"PRINT 'blah'
GO /* blah */";

			Assert.AreEqual(2, BatchSqlParser.SplitBatch(script).Length);
			//why should this throw an exception?
			//UnitTestHelper.AssertThrows<SqlParseException>(() => BatchSqlParser.SplitBatch(script));
		}

		[Test]
		public void TestCommentFollowingGO() {
			var scripts = BatchSqlParser.SplitBatch("/*script1*/GO/*script2*/");
			Assert.AreEqual(2, scripts.Length);
			Assert.AreEqual("/*script1*/", scripts[0]);
			Assert.AreEqual("/*script2*/", scripts[1]);
		}

		[Test]
		public void TestCommentPrecedingGO() {
			var scripts = BatchSqlParser.SplitBatch("/*script1*/GO--script2");
			Assert.AreEqual(2, scripts.Length);
			Assert.AreEqual("/*script1*/", scripts[0]);
			Assert.AreEqual("--script2", scripts[1]);
		}

		[Test]
		public void TestLeadingTablsDashDash() {
			string[] scripts = null;
			scripts = BatchSqlParser.SplitBatch("\t\t\t\t-- comment");
			Assert.AreEqual("\t\t\t\t-- comment", scripts[0]);
		}

		[Test]
		public void TestLeadingTabsParameter() {
			string[] scripts = null;
			scripts = BatchSqlParser.SplitBatch("\t\t\t\t@param");
			Assert.AreEqual("\t\t\t\t@param", scripts[0]);
		}

		[Test]
		public void TestLeadingTabsSingleQuotes() {
			string[] scripts = null;
			scripts = BatchSqlParser.SplitBatch("\t\t\t\t'AddProjectToSourceSafe',");
			Assert.AreEqual("\t\t\t\t'AddProjectToSourceSafe',", scripts[0]);
		}

		[Test]
		public void TestLeadingTabsSlashStar() {
			string[] scripts = null;
			scripts = BatchSqlParser.SplitBatch("\t\t\t\t/* comment */");
			Assert.AreEqual("\t\t\t\t/* comment */", scripts[0]);
		}

		[Test]
		public void TestScriptWithGOTO() {
			var script = @"script 1
GO
script 2
GOTO <-- not a GO <-- niether is this
NOGO <-- also not a GO <-- still no
";
			var scripts = BatchSqlParser.SplitBatch(script);
			Assert.AreEqual(2, scripts.Length);
		}

		[Test]
		public void TestSplitGOInComment() {
			string[] scripts = null;
			scripts = BatchSqlParser.SplitBatch(@"
1:1
-- GO eff yourself
1:2
");
			//shoud be 1 script
			Assert.AreEqual(1, scripts.Length);
		}

		[Test]
		public void TestSplitGOInQuotes() {
			string[] scripts = null;
			scripts = BatchSqlParser.SplitBatch(@"
1:1 ' 
GO
' 1:2
");
			//should be 1 script
			Assert.AreEqual(1, scripts.Length);
		}

		[Test]
		public void TestSplitGONoEndLine() {
			string[] scripts = null;
			scripts = BatchSqlParser.SplitBatch(@"
1:1
1:2
GO");
			//should be 1 script with no 'GO'
			Assert.AreEqual(1, scripts.Length);
			Assert.IsFalse(scripts[0].Contains("GO"));
		}

		[Test]
		public void TestSplitMultipleGOs() {
			string[] scripts = null;
			scripts = BatchSqlParser.SplitBatch(@"
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

		[Test]
		public void IgnoresSlashStarInsideQuotedIdentifier() {
			var scripts = BatchSqlParser.SplitBatch(@"
select 1 as ""/*""
GO
SET ANSI_NULLS OFF
GO
");
			//should be 2 scripts
			Assert.AreEqual(2, scripts.Length);
		}

		[Test]
		public void IgnoresGoInsideBrackets() {
			var scripts = BatchSqlParser.SplitBatch(@"
1:1
select 1 as [GO]
SET ANSI_NULLS OFF
GO
");
			Assert.AreEqual(1, scripts.Length);
		}

		[Test]
		public void IgnoresGoInsideQuotedIdentifier() {
			var scripts = BatchSqlParser.SplitBatch(@"
1:1
select 1 as ""GO""
SET ANSI_NULLS OFF
GO
");
			Assert.AreEqual(1, scripts.Length);
		}
	}
}
