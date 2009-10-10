<TestFixture()> _
Public Class ForeignKeyTester

    <Test()> Public Sub TestScript()
        Dim person As New Table("dbo", "Person")
        person.Columns.Add(New Column("id", "int", False))
        person.Columns.Add(New Column("name", "varchar", 50, False))
        person.Columns.Find("id").Identity = New Identity(1, 1)
        person.Constraints.Add(New model.Constraint("PK_Person", "PRIMARY KEY", "id"))

        Dim address As New Table("dbo", "Address")
        address.Columns.Add(New Column("id", "int", False))
        address.Columns.Add(New Column("personId", "int", False))
        address.Columns.Add(New Column("street", "varchar", 50, False))
        address.Columns.Add(New Column("city", "varchar", 50, False))
        address.Columns.Add(New Column("state", "char", 2, False))
        address.Columns.Add(New Column("zip", "varchar", 5, False))
        address.Columns.Find("id").Identity = New Identity(1, 1)
        address.Constraints.Add(New model.Constraint("PK_Address", "PRIMARY KEY", "id"))

        Dim fk As New ForeignKey(address, "FK_Address_Person", "personId", person, "id", "", "CASCADE")

        TestHelper.ExecSql(person.Script())
        TestHelper.ExecSql(address.Script())
        TestHelper.ExecSql(fk.Script())
        TestHelper.ExecSql("drop table Address")
        TestHelper.ExecSql("drop table Person")
    End Sub

End Class
