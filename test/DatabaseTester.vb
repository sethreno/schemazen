<TestFixture()> _
Public Class DatabaseTester

    <Test()> Public Sub TestScript()
        Dim db As New Database("TEST_TEMP")
        Dim t1 As New Table("dbo", "t1")
        t1.Columns.Add(New Column("col1", "int", False))
        t1.Columns.Add(New Column("col2", "int", False))
        t1.PrimaryKey = New PrimaryKey("pk_t1", "col1,col2")

        Dim t2 As New Table("dbo", "t2")
        t2.Columns.Add(New Column("col1", "int", False))
        t2.Columns.Add(New Column("col2", "int", False))
        t2.Columns.Add(New Column("col3", "int", False))
        t2.PrimaryKey = New PrimaryKey("pk_t2", "col1")

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
        Dim copy As New Database("DFS_COPY")
        copy.Load(TestHelper.GetConnString("DFS_QUOTE"))
        TestHelper.DropDb(copy.Name)
        TestHelper.ExecSql(copy.Script())
        TestHelper.ExecSql(copy.ScriptObjects, copy.Name)

        'TODO automate db comparison
    End Sub

End Class
