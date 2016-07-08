using System;
using System.IO;
using NUnit.Framework;
using SchemaZen.Library;
using SchemaZen.Library.Models;

namespace SchemaZen.Tests
{
    [TestFixture]
    class CommentTester {
        public const string SetupTable0Script = @"
CREATE TABLE [dbo].[TestTable0] (
   [VariantVersionId] [smallint] NOT NULL   ,
   [MetricTypeId] [smallint] NOT NULL   ,
   [RequestId] [bigint] NOT NULL   ,
   [ProfessionalId] [bigint] NOT NULL   ,
   [ZdAppointmentId] [bigint] NOT NULL   
   ,CONSTRAINT [PK_AbAdvancedMetrics_VariantVersionId_MetricTypeId_RequestId] PRIMARY KEY CLUSTERED ([VariantVersionId], [MetricTypeId], [RequestId]),
     CONSTRAINT AK_Uni UNIQUE(MetricTypeId)
)";

        public const string SetupTable1Script = @"
CREATE TABLE [dbo].[TestTable1] (
   [MetricTypeId] [smallint] NOT NULL   
   CONSTRAINT AK_Metric UNIQUE(MetricTypeId)   
)

";

        public const string SetupTableTypeScript = @"
CREATE TYPE [dbo].[TestTableType] AS TABLE(
	[ID] [nvarchar](250) NULL,
	[Value] [numeric](5, 1) NULL,
	[LongNVarchar] [nvarchar](max) NULL
)

";
        public const string SetupFKScript = @"
ALTER TABLE [dbo].[TestTable0]  
  ADD CONSTRAINT TestConstraint
  FOREIGN KEY([VariantVersionId]) REFERENCES [dbo].[TestTable1](MetricTypeId)";

        public const string SetupFuncScript = @"
CREATE FUNCTION TestFunc
(@Description VARCHAR(50),@CreatedDate DateTime)
RETURNS TABLE
AS
  RETURN
    with CharLocations AS (Select OpenParenLoc = CHARINDEX('(',@Description),
         HyphenLoc = CHARINDEX('-',@Description),
         CloseParenLoc = CHARINDEX(')',@Description)),
         SubstringInfos AS (Select StartDateStart = OpenParenLoc + 1,
         StartDateLen = HyphenLoc - OpenParenLoc - 1,
         EndDateStart = HyphenLoc + 1,
         EndDateLen = CloseParenLoc - HyphenLoc - 1
         From CharLocations),
         Substrings As (Select StartDate = SUBSTRING(@Description,StartDateStart,StartDateLen),
         EndDate = SUBSTRING(@Description,EndDateStart,EndDateLen),
         ConcatYear = CASE 
           WHEN (StartDateLen > 5) THEN ''
           ELSE '/' + RIGHT(DATEPART(yyyy,@CreatedDate),2)
         END
         From SubstringInfos)
    (Select StartDate = CONVERT(DATE,StartDate + ConcatYear,1),
    EndDate = CONVERT(DATE,EndDate + ConcatYear,1)
    From Substrings)
";

        public const string SetupProcScript = @"
CREATE PROCEDURE TestProc
(
@dept_name varchar(20)
)
AS
BEGIN
  SELECT * FROM [dbo].[TestTable0]
END";

        public const string SetupRoleScript = @"
CREATE ROLE [TestRole]";

        public const string SetupTrigScript = @"
CREATE TRIGGER TestTrigger ON 
[dbo].[TestTable0]
FOR INSERT
AS
Begin
    SELECT * FROM [dbo].[TestTable0]
End
";

        public const string SetupUserScript = @"
CREATE USER [TestUser] WITHOUT LOGIN WITH DEFAULT_SCHEMA = dbo
";

        public const string SetupViewScript = @"
CREATE VIEW TestView
AS
SELECT * FROM [dbo].[testTable0]
";

    private Database _db;

        [SetUp]
        public void SetUp() {
            _db = new Database("TestAppendComment");
            _db.Connection = ConfigHelper.TestDB.Replace("database=TESTDB", "database=" + _db.Name);
            _db.ExecCreate(true);

            DBHelper.ExecSql(_db.Connection, SetupTable0Script);
            DBHelper.ExecSql(_db.Connection, SetupTable1Script);
            DBHelper.ExecSql(_db.Connection, SetupTableTypeScript);
            DBHelper.ExecSql(_db.Connection, SetupFKScript);
            DBHelper.ExecSql(_db.Connection, SetupFuncScript);
            DBHelper.ExecSql(_db.Connection, SetupProcScript);
            DBHelper.ExecSql(_db.Connection, SetupRoleScript);
            DBHelper.ExecSql(_db.Connection, SetupTrigScript);
            DBHelper.ExecSql(_db.Connection, SetupUserScript);
            DBHelper.ExecSql(_db.Connection, SetupViewScript);
            _db.Dir = _db.Name;
            _db.Load();
            _db.ScriptToDir();
        }

        [TestCase("\\table_types\\", "TYPE_TestTableType.sql")]
        [TestCase("\\foreign_keys\\", "TestTable0.sql")]
        [TestCase("\\functions\\", "TestFunc.sql")]
        [TestCase("\\procedures\\", "TestProc.sql")]
        [TestCase("\\roles\\", "TestRole.sql")]
        [TestCase("\\triggers\\", "TestTrigger.sql")]
        [TestCase("\\tables\\", "TestTable0.sql")]
        [TestCase("\\users\\", "TestUser.sql")]
        [TestCase("\\views\\", "TestView.sql")]
        [TestCase("\\", "schemas.sql")]
        public void TestFilesContainComment(string directory, string fileName)
        {
            Assert.IsTrue(ValidateFirstLineIncludesComment(_db.Name +
                directory + fileName, Database.AutoGenerateComment));
        }

        [TearDown]
        public void TearDown() {
            DBHelper.DropDb(_db.Connection);
            DBHelper.ClearPool(_db.Connection);
            var currPath = ".\\TestAppendComment";
            Directory.Delete(currPath, true);
        }

        bool ValidateFirstLineIncludesComment(string filePath, string matchingStr) {
            var firstLine = File.ReadAllLines(filePath)[0];
            return matchingStr.Contains(firstLine);
        }
    }
}
