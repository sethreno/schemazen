<TestFixture()> _
Public Class ProcTester

    <Test()> Public Sub TestScript()
        Dim t As New Table("dbo", "Address")
        t.Columns.Add(New Column("id", "int", False))
        t.Columns.Add(New Column("street", "varchar", 50, False))
        t.Columns.Add(New Column("city", "varchar", 50, False))
        t.Columns.Add(New Column("state", "char", 2, False))
        t.Columns.Add(New Column("zip", "char", 5, False))
        t.PrimaryKey = New PrimaryKey("PK_Address", "id")

        Dim getAddress As New Proc("dbo", "GetAddress")
        getAddress.Text = _
        "CREATE PROCEDURE [dbo].[GetAddress]" + vbCrLf _
        + "   @id int" + vbCrLf _
        + "AS" + vbCrLf _
        + "   select * from Address where id = @id" + vbCrLf

        Console.WriteLine(t.Script())
        Console.WriteLine(getAddress.Script())

        Using cn As New SqlClient.SqlConnection(ConfigHelper.TestDB)
            cn.Open()
            Using cm As SqlClient.SqlCommand = cn.CreateCommand()
                cm.CommandText = t.Script()
                cm.ExecuteNonQuery()

                cm.CommandText = getAddress.Script()
                cm.ExecuteNonQuery()

                cm.CommandText = "drop table [dbo].[Address]"
                cm.ExecuteNonQuery()

                cm.CommandText = "drop procedure [dbo].[GetAddress]"
                cm.ExecuteNonQuery()
            End Using
        End Using
    End Sub

End Class
