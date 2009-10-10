<TestFixture()> _
Public Class DatabaseTester

    <Test()> Public Sub TestScript()
        Dim db As New Database("TEST_TEMP")
        Dim t1 As New Table("dbo", "t1")
        t1.Columns.Add(New Column("col1", "int", False))
        t1.Columns.Add(New Column("col2", "int", False))
        t1.Constraints.Add(New Constraint("pk_t1", "PRIMARY KEY", "col1,col2"))

        Dim t2 As New Table("dbo", "t2")
        t2.Columns.Add(New Column("col1", "int", False))
        t2.Columns.Add(New Column("col2", "int", False))
        t2.Columns.Add(New Column("col3", "int", False))
        t2.Constraints.Add(New Constraint("pk_t2", "PRIMARY KEY", "col1"))
        t2.Constraints.Add(New Constraint("IX_col3", "UNIQUE", "col3"))
        t2.ForeignKeys.Add(New ForeignKey(t2, "fk_t2_t1", "col2,col3", t1, "col1,col2"))


        db.Tables.Add(t1)
        db.Tables.Add(t2)

        TestHelper.ExecSql(db.Script())
        TestHelper.ExecSql(db.ScriptObjects(), db.Name)

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

            TestHelper.ExecSql("CREATE DATABASE TEST_SOURCE")
            TestHelper.ExecBatchSql(IO.File.ReadAllText(script), "TEST_SOURCE")

            Dim copy As New Database("TEST_COPY")
            copy.Load(TestHelper.GetConnString("TEST_SOURCE"))

            TestHelper.ExecSql(copy.Script())
            TestHelper.ExecSql(copy.ScriptObjects, copy.Name)
            For Each p As Proc In copy.Procs
                TestHelper.ExecSql(p.Script(), copy.Name)
            Next
            For Each f As [Function] In copy.Functions
                TestHelper.ExecSql(f.Script(), copy.Name)
            Next

            'TODO automate db comparison
        Next
    End Sub

    
End Class
