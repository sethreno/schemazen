CREATE TABLE [dbo].[Daily_Discount_Dept_Summary] (
   [Discount_Dept_Summary_ID] [int] NOT NULL
      IDENTITY (1,1),
   [Location_ID] [smallint] NOT NULL,
   [Bus_Date] [smalldatetime] NOT NULL,
   [Daypart_ID] [tinyint] NOT NULL,
   [Dept_ID] [smallint] NOT NULL,
   [Discount_ID] [smallint] NOT NULL,
   [Discount_Ct] [smallint] NOT NULL,
   [Discount_Amt] [smallmoney] NOT NULL,
   [Comp_Discount_Ct] [smallint] NOT NULL,
   [Comp_Discount_Amt] [smallmoney] NOT NULL

   ,CONSTRAINT [PK_Daily_Discount_Dept_Summary] PRIMARY KEY CLUSTERED ([Discount_Dept_Summary_ID])
)


GO
