namespace SchemaZen.Tests {
    internal class TestStrings {
        internal static string TestTable0FileName = "TestTable0.sql";
        internal static string TestTable1FileName = "TestTable1.sql";
        internal static string TableTypeFileName = "TYPE_TestTableType.sql";
        internal static string TestForeignKeyFileName = "TestTable0.sql";
        internal static string TestFunctionFileName = "TestFunc.sql";
        internal static string TestProcedureFileName= "TestProc.sql";
        internal static string TestRoleFileName = "TestRole.sql";
        internal static string TestTrigFileName = "TestTrigger.sql";
        internal static string TestUserFileName = "TestUser.sql";
        internal static string TestViewFileName = "TestView.sql";
        internal static string PropsFileName = "props.sql";
        internal static string SchemasFileName = "schemas.sql";

        internal static string SetupTable0Script = @"
CREATE TABLE [dbo].[TestTable0] (
   [VariantVersionId] [smallint] NOT NULL   ,
   [MetricTypeId] [smallint] NOT NULL   ,
   [RequestId] [bigint] NOT NULL   ,
   [ProfessionalId] [bigint] NOT NULL   ,
   [ZdAppointmentId] [bigint] NOT NULL   
   ,CONSTRAINT [PK_AbAdvancedMetrics_VariantVersionId_MetricTypeId_RequestId] PRIMARY KEY CLUSTERED ([VariantVersionId], [MetricTypeId], [RequestId]),
     CONSTRAINT AK_Uni UNIQUE(MetricTypeId)
)";

        internal static string SetupTable1Script = @"
CREATE TABLE [dbo].[TestTable1] (
   [MetricTypeId] [smallint] NOT NULL   
   CONSTRAINT AK_Metric UNIQUE(MetricTypeId)   
)

";

        internal static string SetupTableTypeScript = @"
CREATE TYPE [dbo].[TestTableType] AS TABLE(
	[ID] [nvarchar](250) NULL,
	[Value] [numeric](5, 1) NULL,
	[LongNVarchar] [nvarchar](max) NULL
)

";
        internal static string SetupFKScript = @"
ALTER TABLE [dbo].[TestTable0]  
  ADD CONSTRAINT TestConstraint
  FOREIGN KEY([VariantVersionId]) REFERENCES [dbo].[TestTable1](MetricTypeId)";

        internal static string SetupFuncScript = @"
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

        internal static string SetupProcScript = @"
CREATE PROCEDURE TestProc
(
@dept_name varchar(20)
)
AS
BEGIN
  SELECT * FROM [dbo].[TestTable0]
END";

        internal static string SetupRoleScript = @"
CREATE ROLE [TestRole]";

        internal static string SetupTrigScript = @"
CREATE TRIGGER TestTrigger ON 
[dbo].[TestTable0]
FOR INSERT
AS
Begin
    SELECT * FROM [dbo].[TestTable0]
End
";

        internal static string SetupUserScript = @"
CREATE USER [TestUser] WITHOUT LOGIN WITH DEFAULT_SCHEMA = dbo
";

        internal static string SetupViewScript = @"
CREATE VIEW TestView
AS
SELECT * FROM [dbo].[testTable0]
";
    }
}