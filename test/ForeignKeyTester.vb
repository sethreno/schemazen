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

        TestHelper.ExecSql(person.Script())
        TestHelper.ExecSql(address.Script())
        TestHelper.ExecSql(fk.Script())
        TestHelper.ExecSql("drop table Address")
        TestHelper.ExecSql("drop table Person")
    End Sub

End Class
