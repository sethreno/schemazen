namespace SchemaZen.Tests {
    public class TestUtils {
        static public string SetupTable0Script = @"
CREATE TABLE [dbo].[TestTable0] (
   [VariantVersionId] [smallint] NOT NULL   ,
   [MetricTypeId] [smallint] NOT NULL   ,
   [RequestId] [bigint] NOT NULL   ,
   [ProfessionalId] [bigint] NOT NULL   ,
   [ZdAppointmentId] [bigint] NOT NULL   
   ,CONSTRAINT [PK_AbAdvancedMetrics_VariantVersionId_MetricTypeId_RequestId] PRIMARY KEY CLUSTERED ([VariantVersionId], [MetricTypeId], [RequestId]),
     CONSTRAINT AK_Uni UNIQUE(MetricTypeId)
)";

        static public string SetupTable1Script = @"
CREATE TABLE [dbo].[TestTable1] (
   [MetricTypeId] [smallint] NOT NULL   
   CONSTRAINT AK_Metric UNIQUE(MetricTypeId)   
)

";

        static public string SetupTableTypeScript = @"
CREATE TYPE [dbo].[TestTableType] AS TABLE(
	[ID] [nvarchar](250) NULL,
	[Value] [numeric](5, 1) NULL,
	[LongNVarchar] [nvarchar](max) NULL
)

";
        static public string SetupFKScript = @"
ALTER TABLE [dbo].[TestTable0]  
  ADD CONSTRAINT TestConstraint
  FOREIGN KEY([VariantVersionId]) REFERENCES [dbo].[TestTable1](MetricTypeId)";

        static public string SetupFuncScript = @"
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

        static public string SetupProcScript = @"
CREATE PROCEDURE TestProc
(
@dept_name varchar(20)
)
AS
BEGIN
  SELECT * FROM [dbo].[TestTable0]
END";

        static public string SetupRoleScript = @"
CREATE ROLE [TestRole]";

        static public string SetupTrigScript = @"
CREATE TRIGGER YourTriggerName ON 
[dbo].[TestTable0]
FOR INSERT
AS
Begin
    SELECT * FROM [dbo].[TestTable0]
End
";

        static public string SetupUserScript = @"
CREATE USER [zocdoc] WITHOUT LOGIN WITH DEFAULT_SCHEMA = dbo
/*ALTER ROLE db_owner ADD MEMBER zocdoc*/ exec sp_addrolemember 'db_owner', 'zocdoc'
/*ALTER ROLE db_datareader ADD MEMBER zocdoc*/ exec sp_addrolemember 'db_datareader', 'zocdoc'
/*ALTER ROLE db_datawriter ADD MEMBER zocdoc*/ exec sp_addrolemember 'db_datawriter', 'zocdoc'
";

        static public string SetupViewScript = @"
CREATE VIEW test
AS
SELECT * FROM [dbo].[testTable0]
";
    }
}