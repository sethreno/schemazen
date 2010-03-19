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

        [Test()]
        public void TestLeadingTabsSingleQuotes() {
            string[] scripts = null;
            scripts = DBHelper.SplitBatchSql("\t\t\t\t'AddProjectToSourceSafe',");
            Assert.AreEqual("\t\t\t\t'AddProjectToSourceSafe',", scripts[0]);
        }

        [Test()]
        public void TestLeadingTabsSlashStar() {
            string[] scripts = null;
            scripts = DBHelper.SplitBatchSql("\t\t\t\t/* comment */");
            Assert.AreEqual("\t\t\t\t/* comment */", scripts[0]);
        }

        [Test()]
        public void TestLeadingTablsDashDash() {
            string[] scripts = null;
            scripts = DBHelper.SplitBatchSql("\t\t\t\t-- comment");
            Assert.AreEqual("\t\t\t\t-- comment", scripts[0]);
        }

        [Test()]
        public void TestLeadingTabsParameter() {
            string[] scripts = null;
            scripts = DBHelper.SplitBatchSql("\t\t\t\t@param");
            Assert.AreEqual("\t\t\t\t@param", scripts[0]);
        }

        [Test()]
        public void TestScriptWithGOTO() {
            var script = @"script 1
GO
script 2
GOTO <-- not a GO <-- niether is this
NOGO <-- also not a GO <-- still no
";
            var scripts = DBHelper.SplitBatchSql(script);
            Assert.AreEqual(2, scripts.Length);            
        }

        [Test()]
        public void TestCommentPrecedingGO() {
            string[] scripts = DBHelper.SplitBatchSql("/*script1*/GO--script2");
            Assert.AreEqual(2, scripts.Length);
            Assert.AreEqual("/*script1*/", scripts[0]);
            Assert.AreEqual("--script2", scripts[1]);           
        }

        [Test()]
        public void TestCommentFollowingGO() {
            string[] scripts = DBHelper.SplitBatchSql("/*script1*/GO/*script2*/");
            Assert.AreEqual(2, scripts.Length);
            Assert.AreEqual("/*script1*/", scripts[0]);
            Assert.AreEqual("/*script2*/", scripts[1]);
        }

        #region Tests from Subtext.Scripting

        [Test]
        public void CanParseGoWithDashDashCommentAfter() {
            const string script = @"SELECT * FROM foo;
 GO --  Hello Phil
CREATE PROCEDURE dbo.Test AS SELECT * FROM foo";
            string[] scripts = DBHelper.SplitBatchSql(script);
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
            string[] scripts = DBHelper.SplitBatchSql(script);
            Assert.AreEqual(1, scripts.Length, "This contains a comment and no scripts.");
        }

        [Test]
        public void SlashStarCommentAfterGoThrowsException() {
            const string script = @"PRINT 'blah'
GO /* blah */";

            Assert.AreEqual(2, DBHelper.SplitBatchSql(script).Length);
            //why should this throw an exception?
            //UnitTestHelper.AssertThrows<SqlParseException>(() => DBHelper.SplitBatchSql(script));
        }

        [Test]
        public void CanParseSuccessiveGoStatements() {
            const string script = @"GO
GO";
            string[] scripts = DBHelper.SplitBatchSql(script);
            Assert.AreEqual(0, scripts.Length, "Expected no scripts since they would be empty.");
        }

        [Test]
        public void SemiColonDoesNotSplitScript() {
            const string script = "CREATE PROC Blah AS SELECT FOO; SELECT Bar;";
            string[] scripts = DBHelper.SplitBatchSql(script);
            Assert.AreEqual(1, scripts.Length, "Expected no scripts since they would be empty.");
        }

        [Test]
        public void CanParseQuotedCorrectly() {
            const string script = @"INSERT INTO #Indexes
        EXEC sp_helpindex 'dbo.subtext_URLs'";

            string[] scripts = DBHelper.SplitBatchSql(script);
            Assert.AreEqual(script, scripts[0], "Script text should not be modified");
        }

        [Test]
        public void CanParseSimpleScript() {
            string script = "Test" + Environment.NewLine + "go";
            string[] scripts = DBHelper.SplitBatchSql(script);
            Assert.AreEqual(1, scripts.Length);
            Assert.AreEqual("Test" + Environment.NewLine, scripts[0]);
        }

        [Test]
        public void CanParseCommentBeforeGoStatement() {
            const string script = @"SELECT FOO
/*TEST*/ GO
BAR";
            string[] scripts = DBHelper.SplitBatchSql(script);
            Assert.AreEqual(2, scripts.Length);
        }

        [Test]
        public void CanParseCommentWithQuoteChar() {
            const string script = @"/* Add the Url column to the subtext_Log table if it doesn't exist */
        ADD [Url] VARCHAR(255) NULL
GO
                AND             COLUMN_NAME = 'BlogGroup') IS NULL";
            string[] scripts = DBHelper.SplitBatchSql(script);
            Assert.AreEqual(2, scripts.Length);
        }

        [Test]
        public void CanParseDashDashCommentWithQuoteChar() {
            const string script = @"-- Add the Url column to the subtext_Log table if it doesn't exist
SELECT * FROM BLAH
GO
PRINT 'FOO'";
            string[] scripts = DBHelper.SplitBatchSql(script);
            Assert.AreEqual(2, scripts.Length);
        }

        [Test]
        public void CanParseLineEndingInDashDashComment() {
            const string script = @"SELECT * FROM BLAH -- Comment
GO
FOOBAR
GO";
            string[] scripts = DBHelper.SplitBatchSql(script);
            Assert.AreEqual(2, scripts.Length);
        }

        [Test]
        public void CanParseSimpleScriptEndingInNewLine() {
            string script = "Test" + Environment.NewLine + "GO" + Environment.NewLine;
            string[] scripts = DBHelper.SplitBatchSql(script);
            Assert.AreEqual(1, scripts.Length);
            Assert.AreEqual("Test" + Environment.NewLine, scripts[0]);
        }

        [Test]
        public void MultiLineQuoteShouldNotIgnoreDoubleQuote() {
            string script = "PRINT '" + Environment.NewLine
                            + "''" + Environment.NewLine
                            + "GO" + Environment.NewLine
                            + "/*" + Environment.NewLine
                            + "GO"
                            + "'";

            string[] scripts = DBHelper.SplitBatchSql(script);

            Assert.AreEqual(1, scripts.Length);
            Assert.AreEqual(script, scripts[0]);
        }

        [Test]
        public void MultiLineQuoteShouldNotBeSplitByGoKeyword() {
            string script = "PRINT '" + Environment.NewLine
                            + "GO" + Environment.NewLine
                            + "SELECT * FROM BLAH" + Environment.NewLine
                            + "GO" + Environment.NewLine
                            + "'";

            string[] scripts = DBHelper.SplitBatchSql(script);

            Assert.AreEqual(script, scripts[0]);
            Assert.AreEqual(1, scripts.Length, "expected only one script");
        }

        /// <summary>
        /// Makes sure that ParseScript parses correctly.
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
            string[] scripts = DBHelper.SplitBatchSql(script);
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
        
        #endregion
    }
}