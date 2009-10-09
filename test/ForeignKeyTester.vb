<TestFixture()> _
Public Class ForeignKeyTester

    <Test()> Public Sub TestScript()
        Dim person As New Table("dbo", "Person")
        person.Columns.Add(New Column("id", "int", False))
        person.Columns.Add(New Column("name", "varchar", 50, False))
        person.PrimaryKey = New PrimaryKey("PK_Person", "id")

        Dim address As New Table("dbo", "Address")
        address.Columns.Add(New Column("id", "int", False))
        address.Columns.Add(New Column("street", "varchar", 50, False))
        address.Columns.Add(New Column("city", "varchar", 50, False))
        address.Columns.Add(New Column("state", "char", 2, False))
        address.Columns.Add(New Column("zip", "varchar", 5, False))
        address.PrimaryKey = New PrimaryKey("PK_Address", "id")

        Dim fk As New ForeignKey(address, "FK_Address_Person", "id", person, "id", "", "DELETE")
        Console.Write(person.Script())
        Console.Write(address.Script())
        Console.Write(fk.Script())

        Using cn As New SqlClient.SqlConnection(ConfigHelper.TestDB)
            cn.Open()
            Using cm As SqlClient.SqlCommand = cn.CreateCommand()
                cm.CommandText = person.Script()
                cm.ExecuteNonQuery()

                cm.CommandText = address.Script()
                cm.ExecuteNonQuery()

                cm.CommandText = fk.Script()
                cm.ExecuteNonQuery()

                cm.CommandText = "drop table Address"
                cm.ExecuteNonQuery()

                cm.CommandText = "drop table Person"
                cm.ExecuteNonQuery()
            End Using
        End Using
    End Sub

End Class
