<TestFixture()> _
Public Class ProcTester

    <Test()> Public Sub TestScript()
        Dim t As New Table("dbo", "Address")
        t.Columns.Add(New Column("id", "int", False))
        t.Columns.Add(New Column("street", "varchar", 50, False))
        t.Columns.Add(New Column("city", "varchar", 50, False))
        t.Columns.Add(New Column("state", "char", 2, False))
        t.Columns.Add(New Column("zip", "char", 5, False))
        t.Constraints.Add(New model.Constraint("PK_Address", "PRIMARY KEY", "id"))

        Dim getAddress As New Routine("dbo", "GetAddress")
        getAddress.Text = _
        "CREATE PROCEDURE [dbo].[GetAddress]" + vbCrLf _
        + "   @id int" + vbCrLf _
        + "AS" + vbCrLf _
        + "   select * from Address where id = @id" + vbCrLf

        TestHelper.ExecSql(t.ScriptCreate())
        TestHelper.ExecSql(getAddress.ScriptCreate)
        TestHelper.ExecSql("drop table [dbo].[Address]")
        TestHelper.ExecSql("drop procedure [dbo].[GetAddress]")
    End Sub

End Class
