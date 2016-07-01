using System.IO;
using NUnit.Framework;
using SchemaZen.Library;
using SchemaZen.Library.Models;

namespace SchemaZen.Tests
{
    class CommentTester 
    {
        [Test]
        public void TestScriptTableType()
        {
            var setupTable = @"
CREATE TABLE [dbo].[AbAdvancedMetrics] (
   [VariantVersionId] [smallint] NOT NULL   ,
   [MetricTypeId] [smallint] NOT NULL   ,
   [RequestId] [bigint] NOT NULL   ,
   [ProfessionalId] [bigint] NOT NULL   ,
   [ZdAppointmentId] [bigint] NOT NULL   

   ,CONSTRAINT [PK_AbAdvancedMetrics_VariantVersionId_MetricTypeId_RequestId] PRIMARY KEY CLUSTERED ([VariantVersionId], [MetricTypeId], [RequestId]),
     CONSTRAINT AK_Uni UNIQUE(MetricTypeId)
)

";

            var setupOtherTable = @"
CREATE TABLE [dbo].[OtherTestTable] (
   [MetricTypeId] [smallint] NOT NULL   
   CONSTRAINT AK_Metric UNIQUE(MetricTypeId)   
)

";

            var setupTableType = @"
CREATE TYPE [dbo].[MyTableType] AS TABLE(
	[ID] [nvarchar](250) NULL,
	[Value] [numeric](5, 1) NULL,
	[LongNVarchar] [nvarchar](max) NULL
)

";
            var setupForeignKeys = @"
ALTER TABLE [dbo].[AbAdvancedMetrics]  
  ADD CONSTRAINT FK_WOW
  FOREIGN KEY([VariantVersionId]) REFERENCES [dbo].[OtherTestTable](MetricTypeId)";

            var setupFunctions = @"
CREATE FUNCTION AccountingAuditorGetDatesFromBillItemDescription_fn
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

            var setupProcedures = @"
CREATE PROCEDURE TestProc
(
@dept_name varchar(20)
)
AS
BEGIN
  SELECT * FROM [dbo].[AbAdvancedMetrics]
END";

            var setupRoles = @"
CREATE ROLE [aspnet_Membership_BasicAccess]";

            var setupTriggers = @"
CREATE TRIGGER YourTriggerName ON 
[dbo].[AbAdvancedMetrics]
FOR INSERT
AS
Begin
    SELECT * FROM [dbo].[AbAdvancedMetrics]
End
";

            var setupUsers = @"
CREATE USER [zocdoc] WITHOUT LOGIN WITH DEFAULT_SCHEMA = dbo
/*ALTER ROLE db_owner ADD MEMBER zocdoc*/ exec sp_addrolemember 'db_owner', 'zocdoc'
/*ALTER ROLE db_datareader ADD MEMBER zocdoc*/ exec sp_addrolemember 'db_datareader', 'zocdoc'
/*ALTER ROLE db_datawriter ADD MEMBER zocdoc*/ exec sp_addrolemember 'db_datawriter', 'zocdoc'
";

            var setupView = @"
CREATE VIEW test
AS
SELECT * FROM [dbo].[AbAdvancedMetrics]
";




            var db = new Database("TestScriptTableType");

            db.Connection = ConfigHelper.TestDB.Replace("database=TESTDB", "database=" + db.Name);

            db.ExecCreate(true);

            DBHelper.ExecSql(db.Connection, setupTable);
            DBHelper.ExecSql(db.Connection, setupOtherTable);
            DBHelper.ExecSql(db.Connection, setupTableType);
            DBHelper.ExecSql(db.Connection, setupForeignKeys);
            DBHelper.ExecSql(db.Connection, setupFunctions);
            DBHelper.ExecSql(db.Connection, setupRoles);
            DBHelper.ExecSql(db.Connection, setupTriggers);
            DBHelper.ExecSql(db.Connection, setupView);
            DBHelper.ExecSql(db.Connection, setupProcedures);


            db.Dir = db.Name;
            db.Load();

            db.ScriptToDir();

            Assert.IsTrue(File.Exists(db.Name + "\\table_types\\TYPE_MyTableType.sql"));

        }
    }
}
