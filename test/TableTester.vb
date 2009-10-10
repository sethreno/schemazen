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
        Console.Write(t1.Script())

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

        Console.WriteLine(t.Script())
        TestHelper.ExecSql(t.Script())
        TestHelper.ExecSql("drop table [dbo].[AllTypesTest]")
    End Sub

    <Test()> <ExpectedException(GetType(NotSupportedException))> _
    Public Sub TestScriptNonSupportedColumn()
        Dim t As New Table("dbo", "bla")
        t.Columns.Add(New Column("a", "madeuptype", True))
        t.Script()
    End Sub

End Class
