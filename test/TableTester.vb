<TestFixture()> _
Public Class TableTester

    <Test()> Public Sub TestCompare()
        Dim t1 As New Table("dbo", "Test")
        Dim t2 As New Table("dbo", "Test")
        Dim diff As TableDiff

        'test equal
        t1.Columns.Add(New Column("first", "varchar", 30))
        t2.Columns.Add(New Column("first", "varchar", 30))
        diff = t1.Compare(t2)
        Assert.IsNotNull(diff)
        Assert.IsFalse(diff.IsDiff)

        'test add
        t1.Columns.Add(New Column("second", "varchar", 30))
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

End Class
