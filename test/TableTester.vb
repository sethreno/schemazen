<TestFixture()> _
Public Class TableTester

    <Test()> Public Sub TestCompare()
        Dim t1 As New Table("dbo", "Test")
        Dim t2 As New Table("dbo", "Test")
        Dim diff As TableDiff

        'test equal
        t1.Columns.Add(New Column("first", "varchar", 30, False))
        t2.Columns.Add(New Column("first", "varchar", 30, False))
        t1.Constraints.Add(New Constraint("PK_Test", "PRIMARY KEY", "first"))
        t2.Constraints.Add(New Constraint("PK_Test", "PRIMARY KEY", "first"))
        
        diff = t1.Compare(t2)
        Assert.IsNotNull(diff)
        Assert.IsFalse(diff.IsDiff)

        'test add
        t1.Columns.Add(New Column("second", "varchar", 30, False))
        diff = t1.Compare(t2)
        Assert.IsTrue(diff.IsDiff)
        Assert.AreEqual(1, diff.ColumnsAdded.Count)

        'test delete
        diff = t2.Compare(t1)
        Assert.IsTrue(diff.IsDiff)
        Assert.AreEqual(1, diff.ColumnsDroped.Count)

        'test diff
        t1.Columns.Items(0).Length = 20
        diff = t1.Compare(t2)
        Assert.IsTrue(diff.IsDiff)
        Assert.AreEqual(1, diff.ColumnsDiff.Count)

        Console.WriteLine("--- create ----")
        Console.Write(t1.ScriptCreate())

        Console.WriteLine("--- migrate up ---")
        Console.Write(t1.Compare(t2).Script())

        Console.WriteLine("--- migrate down ---")
        Console.Write(t2.Compare(t1).Script())
    End Sub

    <Test()> Public Sub TestScript()
        'create a table with all known types, script it, and execute the script
        Dim t As New Table("dbo", "AllTypesTest")
        t.Columns.Add(New Column("a", "bigint", False))
        t.Columns.Add(New Column("b", "binary", 50, False))
        t.Columns.Add(New Column("c", "bit", False))
        t.Columns.Add(New Column("d", "char", 10, False))
        t.Columns.Add(New Column("e", "datetime", False))
        t.Columns.Add(New Column("f", "decimal", 18, 0, False))
        t.Columns.Add(New Column("g", "float", False))
        t.Columns.Add(New Column("h", "image", False))
        t.Columns.Add(New Column("i", "int", False))
        t.Columns.Add(New Column("j", "money", False))
        t.Columns.Add(New Column("k", "nchar", 10, False))
        t.Columns.Add(New Column("l", "ntext", False))
        t.Columns.Add(New Column("m", "numeric", 18, 0, False))
        t.Columns.Add(New Column("n", "nvarchar", 50, False))
        t.Columns.Add(New Column("o", "nvarchar", -1, False))
        t.Columns.Add(New Column("p", "real", False))
        t.Columns.Add(New Column("q", "smalldatetime", False))
        t.Columns.Add(New Column("r", "smallint", False))
        t.Columns.Add(New Column("s", "smallmoney", False))
        t.Columns.Add(New Column("t", "sql_variant", False))
        t.Columns.Add(New Column("u", "text", False))
        t.Columns.Add(New Column("v", "timestamp", False))
        t.Columns.Add(New Column("w", "tinyint", False))
        t.Columns.Add(New Column("x", "uniqueidentifier", False))
        t.Columns.Add(New Column("y", "varbinary", 50, False))
        t.Columns.Add(New Column("z", "varbinary", -1, False))
        t.Columns.Add(New Column("aa", "varchar", 50, True, New [Default]("DF_AllTypesTest_aa", "'asdf'")))
        t.Columns.Add(New Column("bb", "varchar", -1, True))
        t.Columns.Add(New Column("cc", "xml", True))

        Console.WriteLine(t.ScriptCreate())
        TestHelper.ExecSql(t.ScriptCreate())
        TestHelper.ExecSql("drop table [dbo].[AllTypesTest]")
    End Sub

    <Test()> <ExpectedException(GetType(NotSupportedException))> _
    Public Sub TestScriptNonSupportedColumn()
        Dim t As New Table("dbo", "bla")
        t.Columns.Add(New Column("a", "madeuptype", True))
        t.ScriptCreate()
    End Sub

    <Test()> Public Sub TestExportData()
        Dim t As New Table("dbo", "Status")
        t.Columns.Add(New Column("id", "int", False))
        t.Columns.Add(New Column("code", "char", 1, False))
        t.Columns.Add(New Column("description", "varchar", 20, False))
        t.Columns.Find("id").Identity = New Identity(1, 1)
        t.Constraints.Add(New Constraint("PK_Status", "PRIMARY KEY", "id"))

        Dim conn As String = TestHelper.GetConnString("TESTDB")
        DBHelper.DropDb(conn)
        DBHelper.CreateDb(conn)
        DBHelper.ExecBatchSql(conn, t.ScriptCreate())

        DBHelper.ExecBatchSql(conn, _
        "SET IDENTITY_INSERT [Status] ON" + vbCrLf _
        + "GO" + vbCrLf _
        + "insert into Status (id,code,description) values (1,'R','Ready')" + vbCrLf _
        + "insert into Status (id,code,description) values (2,'P','Processing')" + vbCrLf _
        + "insert into Status (id,code,description) values (3,'F','Frozen')" + vbCrLf _
        + "GO" + vbCrLf _
        + "SET IDENTITY_INSERT [Status] OFF" + vbCrLf _
        + "GO" + vbCrLf)

        Dim data As String = t.ExportData(conn)
        Assert.IsFalse(String.IsNullOrEmpty(data))

        Dim dataList As List(Of List(Of String)) = TabDataToList(data)
        Assert.AreEqual("1", dataList(0)(0))
        Assert.AreEqual("R", dataList(0)(1))
        Assert.AreEqual("Ready", dataList(0)(2))
        Assert.AreEqual("2", dataList(1)(0))
        Assert.AreEqual("P", dataList(1)(1))
        Assert.AreEqual("Processing", dataList(1)(2))
        Assert.AreEqual("3", dataList(2)(0))
        Assert.AreEqual("F", dataList(2)(1))
        Assert.AreEqual("Frozen", dataList(2)(2))
    End Sub

    Private Function TabDataToList(ByVal data As String) As List(Of List(Of String))
        Dim lines As New List(Of List(Of String))
        For Each line As String In data.Split(Chr(10))
            lines.Add(New List(Of String))
            For Each field As String In line.Split(Chr(9))
                lines(lines.Count - 1).Add(field)
            Next
        Next
        'remove the \r from the end of the last field of each line
        For Each line As List(Of String) In lines
            If line.Last.Length = 0 Then Continue For
            line(line.Count - 1) = line.Last.Remove(line.Last.Length - 1, 1)
        Next
        Return lines
    End Function
End Class
