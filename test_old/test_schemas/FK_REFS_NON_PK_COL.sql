/****** Object:  Table [dbo].[Daily_Discount_Dept_Summary]    Script Date: 2/28/2014 11:12:11 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Daily_Discount_Dept_Summary](
	[Discount_Dept_Summary_ID] [int] IDENTITY(1,1) NOT NULL,
	[Location_ID] [smallint] NOT NULL,
	[Bus_Date] [smalldatetime] NOT NULL,
	[Daypart_ID] [tinyint] NOT NULL,
	[Dept_ID] [smallint] NOT NULL,
	[Discount_ID] [smallint] NOT NULL,
	[Discount_Ct] [smallint] NOT NULL,
	[Discount_Amt] [smallmoney] NOT NULL,
	[Comp_Discount_Ct] [smallint] NOT NULL,
	[Comp_Discount_Amt] [smallmoney] NOT NULL,
 CONSTRAINT [PK_Daily_Discount_Dept_Summary] PRIMARY KEY CLUSTERED 
(
	[Discount_Dept_Summary_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[MASTER_LOCATIONS]    Script Date: 2/28/2014 11:12:11 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[MASTER_LOCATIONS](
	[Location_ID] [smallint] IDENTITY(1,1) NOT NULL,
	[Location] [int] NOT NULL,
	[Location_Name] [varchar](30) NULL,
	[Short_Name] [char](8) NULL,
	[Location_Status] [tinyint] NOT NULL,
	[Concept_No] [tinyint] NOT NULL,
	[Location_Pay_Start_Weekday] [tinyint] NULL,
	[Location_Pay_Period_Days] [tinyint] NULL,
	[Corp_Pay_Period_Days] [tinyint] NOT NULL,
	[Loc_PayCycle_ID] [tinyint] NOT NULL,
	[Corp_PayCycle_ID] [tinyint] NOT NULL,
	[Location_Active_Date] [smalldatetime] NOT NULL,
	[Location_Retire_Date] [smalldatetime] NULL,
	[Location_Closed_Date] [smalldatetime] NULL,
	[Location_State] [char](2) NOT NULL,
	[Location_Country] [char](3) NOT NULL,
	[Timezone_Code] [smallint] NOT NULL,
	[Location_Report_1] [tinyint] NULL,
	[Alt_Location_No] [varchar](12) NOT NULL,
	[Employee_Verify_Method] [tinyint] NOT NULL,
	[OT_Calc_Method] [tinyint] NOT NULL,
	[Minimum_Wage] [smallmoney] NOT NULL,
	[Minimum_Tip_Credit_Wage] [smallmoney] NOT NULL,
	[Tip_Credit_Differential] [smallmoney] NOT NULL,
	[Minimum_OT_Wage] [smallmoney] NOT NULL,
	[OT_Multiplier_1] [smallmoney] NOT NULL,
	[OT_Multiplier_2] [smallmoney] NOT NULL,
	[Under_18_Min_Wage] [smallmoney] NOT NULL,
	[Tipped_Emp_Min_Wage] [smallmoney] NOT NULL,
	[Minimum_Train_Wage] [smallmoney] NULL,
	[Payrate_Chgs_Retroactive] [tinyint] NOT NULL,
	[POS_Type] [tinyint] NOT NULL,
	[Payroll_SW_Type] [tinyint] NOT NULL,
	[Payroll_Reference_Date] [smalldatetime] NOT NULL,
	[Annual_Pay_Cycles] [smallint] NOT NULL,
	[Gross_Totals_Flag] [tinyint] NOT NULL,
	[Dept_Totals_Flag] [tinyint] NOT NULL,
	[DSR_Format] [tinyint] NOT NULL,
	[Reporting_Type] [tinyint] NOT NULL,
	[Manual_CC_Flag] [tinyint] NOT NULL,
	[Petty_Cash_Handling] [tinyint] NOT NULL,
	[Sales_Cats_By_Location] [tinyint] NOT NULL,
	[Daypart_Validate_Method] [tinyint] NOT NULL,
	[Min_Daypart_ID] [tinyint] NOT NULL,
	[Max_Daypart_ID] [tinyint] NOT NULL,
	[Pay_Cat_Validate_Method] [tinyint] NOT NULL,
	[Employee_Validate_Method] [tinyint] NOT NULL,
	[Jobcode_Validate_Method] [tinyint] NOT NULL,
	[Item_Validate_Method] [tinyint] NOT NULL,
	[Discount_Validate_Method] [tinyint] NOT NULL,
	[Cashier_System_Flag] [tinyint] NOT NULL,
	[FM_Status] [tinyint] NOT NULL,
	[FM_AltVendKey] [tinyint] NOT NULL,
	[Force_Load_Payroll] [tinyint] NOT NULL,
	[PR_Tip_Display_Method] [tinyint] NOT NULL,
	[Upload_Invoice_Status] [tinyint] NOT NULL,
	[Locality_ID] [tinyint] NOT NULL,
	[CC_Tips_In_Cash] [tinyint] NOT NULL,
	[Employee_Updates_Date] [datetime] NOT NULL,
	[POS_Conversion_Date] [datetime] NOT NULL,
	[nonLoc_Flag] [tinyint] NOT NULL,
	[DSR_LockUpdtWhenFinal] [tinyint] NOT NULL,
	[Paycard_Verify_Method] [tinyint] NOT NULL,
	[Break_Compliance] [tinyint] NOT NULL,
	[LastEmpLoadDT] [smalldatetime] NULL,
	[PR_DoNotLoadTimeKeeping] [tinyint] NOT NULL,
	[LSF_FCast_Method] [varchar](3) NOT NULL,
	[LSF_Max_Shift_Hrs] [decimal](4, 2) NOT NULL,
	[LSF_FIFO_Limit] [decimal](4, 2) NOT NULL,
	[LSF_Gap_Hrs] [decimal](3, 2) NOT NULL,
	[CC_Processing_Flag] [tinyint] NOT NULL,
	[BeverageExport_Flag] [int] NOT NULL,
	[LSF_Theo_At_Dataload] [tinyint] NOT NULL,
	[LSF_FCast_Cur_Sls] [tinyint] NOT NULL,
	[LSS_New_Sched_Options] [varchar](24) NOT NULL,
	[PR_Last_Audit_Offset] [bigint] NOT NULL,
	[PR_Last_Audit_Datetime] [smalldatetime] NOT NULL,
	[ASI_SALE_ID] [int] NOT NULL,
	[LineCheck_Probe_Required] [bit] NOT NULL,
 CONSTRAINT [PK_MASTER_LOCATIONS_1__14] PRIMARY KEY NONCLUSTERED 
(
	[Location] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 90) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_LOCATION_ID] ON [dbo].[MASTER_LOCATIONS]
(
	[Location_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO


-- here's the fk that exposes the bug Location_ID is not part ofthe primary key on MASTER_LOCATIONS but there is a UNIQUE index defined for it so it's supported by SQL Server
ALTER TABLE [dbo].[Daily_Discount_Dept_Summary]  WITH CHECK ADD  CONSTRAINT [FK_DAILY_DISCOUNT_DEPT_SUMMARY_Master_Locations] FOREIGN KEY([Location_ID])
REFERENCES [dbo].[MASTER_LOCATIONS] ([Location_ID])
GO
ALTER TABLE [dbo].[Daily_Discount_Dept_Summary] CHECK CONSTRAINT [FK_DAILY_DISCOUNT_DEPT_SUMMARY_Master_Locations]
GO
