<TestFixture()> _
Public Class DatabaseTester

    <Test()> Public Sub TestScript()
        Dim db As New Database("TEST_TEMP")
        Dim t1 As New Table("dbo", "t1")
        t1.Columns.Add(New Column("col1", "int", False))
        t1.Columns.Add(New Column("col2", "int", False))
        t1.Constraints.Add(New Constraint("pk_t1", "PRIMARY KEY", "col1,col2"))
        t1.FindConstraint("pk_t1").Clustered = True

        Dim t2 As New Table("dbo", "t2")
        t2.Columns.Add(New Column("col1", "int", False))
        t2.Columns.Add(New Column("col2", "int", False))
        t2.Columns.Add(New Column("col3", "int", False))
        t2.Constraints.Add(New Constraint("pk_t2", "PRIMARY KEY", "col1"))
        t2.FindConstraint("pk_t2").Clustered = True
        t2.Constraints.Add(New Constraint("IX_col3", "UNIQUE", "col3"))

        db.ForeignKeys.Add(New ForeignKey(t2, "fk_t2_t1", "col2,col3", t1, "col1,col2"))

        db.Tables.Add(t1)
        db.Tables.Add(t2)

        TestHelper.DropDb("TEST_TEMP")
        Data.SqlClient.SqlConnection.ClearAllPools()
        TestHelper.ExecBatchSql(db.ScriptCreate(), "master")

        Dim db2 As New Database()
        db2.Load(TestHelper.GetConnString("TEST_TEMP"))

        TestHelper.DropDb("TEST_TEMP")

        For Each t As Table In db.Tables
            Assert.IsNotNull(db2.FindTable(t.Name))
            Assert.IsFalse(db2.FindTable(t.Name).Compare(t).IsDiff)
        Next
    End Sub

    <Test()> Public Sub TestCopy()
        For Each script As String In IO.Directory.GetFiles(ConfigHelper.TestSchemaDir)
            TestHelper.DropDb("TEST_SOURCE")
            TestHelper.DropDb("TEST_COPY")

            'create the db from sql script
            TestHelper.ExecSql("CREATE DATABASE TEST_SOURCE")
            TestHelper.ExecBatchSql(IO.File.ReadAllText(script), "TEST_SOURCE")

            'load the model from newly created db and create a copy
            Dim copy As New Database("TEST_COPY")
            copy.Load(TestHelper.GetConnString("TEST_SOURCE"))
            Data.SqlClient.SqlConnection.ClearAllPools()
            TestHelper.ExecBatchSql(copy.ScriptCreate(), "master")

            'compare the dbs to make sure they are the same
            Dim cmd As String = String.Format("{0}\SQLDBDiffConsole.exe {1} {2} {0}\{3}" _
                , ConfigHelper.SqlDbDiffPath _
                , "localhost\SQLEXPRESS TEST_COPY   NULL NULL Y" _
                , "localhost\SQLEXPRESS TEST_SOURCE NULL NULL Y" _
                , "SqlDbDiff.XML CompareResult.txt null")
            Shell(cmd, AppWinStyle.NormalFocus, True)
            Assert.AreEqual("no difference", IO.File.ReadAllLines("CompareResult.txt")(0))
        Next
    End Sub

    <Test()> Public Sub TestDiffScript()
        TestHelper.DropDb("TEST_SOURCE")
        TestHelper.DropDb("TEST_COPY")

        'create the dbs from sql script
        Dim script As String = IO.File.ReadAllText(ConfigHelper.TestSchemaDir + "\BOP_QUOTE.sql")
        TestHelper.ExecSql("CREATE DATABASE TEST_SOURCE")
        TestHelper.ExecBatchSql(script, "TEST_SOURCE")

        script = IO.File.ReadAllText(ConfigHelper.TestSchemaDir + "\BOP_QUOTE_2.sql")
        TestHelper.ExecSql("CREATE DATABASE TEST_COPY")
        TestHelper.ExecBatchSql(script, "TEST_COPY")

        Dim source As New Database("TEST_SOURCE")
        source.Load(TestHelper.GetConnString("TEST_SOURCE"))

        Dim copy As New Database("TEST_COPY")
        copy.Load(TestHelper.GetConnString("TEST_COPY"))

        'execute migration script to make SOURCE the same as COPY
        Dim diff As DatabaseDiff = copy.Compare(source)
        TestHelper.ExecBatchSql(diff.Script(), "TEST_SOURCE")

        'compare the dbs to make sure they are the same
        Dim cmd As String = String.Format("{0}\SQLDBDiffConsole.exe {1} {2} {0}\{3}" _
            , ConfigHelper.SqlDbDiffPath _
            , "localhost\SQLEXPRESS TEST_COPY   NULL NULL Y" _
            , "localhost\SQLEXPRESS TEST_SOURCE NULL NULL Y" _
            , "SqlDbDiff.XML CompareResult.txt null")
        Shell(cmd, AppWinStyle.NormalFocus, True)
        Assert.AreEqual("no difference", IO.File.ReadAllLines("CompareResult.txt")(0))
    End Sub

    <Test()> Public Sub TestFindTableRegEx()
        Dim db As New Database()
        db.Tables.Add(New Table("dbo", "cmicDeductible"))
        db.Tables.Add(New Table("dbo", "cmicZipCode"))
        db.Tables.Add(New Table("dbo", "cmicState"))
        db.Tables.Add(New Table("dbo", "Policy"))
        db.Tables.Add(New Table("dbo", "Location"))
        db.Tables.Add(New Table("dbo", "Rate"))

        Assert.AreEqual(3, db.FindTablesRegEx("^cmic").Count)
        Assert.AreEqual(1, db.FindTablesRegEx("Location").Count)
    End Sub
End Class
