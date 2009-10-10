Public Class ForeignKey
    Public Table As Table
    Public Name As String
    Public Columns As New List(Of String)
    Public RefTable As Table
    Public RefColumns As New List(Of String)
    Public OnUpdate As String
    Public OnDelete As String

    Public Sub New(ByVal name As String)
        Me.Name = name
    End Sub

    Public Sub New(ByVal table As Table, ByVal name As String, _
                   ByVal columns As String, ByVal refTable As Table, ByVal refColumns As String)
        Me.New(table, name, columns, refTable, refColumns, "", "")
    End Sub

    Public Sub New(ByVal table As Table, ByVal name As String, _
                   ByVal columns As String, ByVal refTable As Table, ByVal refColumns As String, _
                   ByVal onUpdate As String, ByVal onDelete As String)
        Me.Table = table
        Me.Name = name
        Me.Columns = New List(Of String)(columns.Split(","c))
        Me.RefTable = refTable
        Me.RefColumns = New List(Of String)(refColumns.Split(","c))
        Me.OnUpdate = onUpdate
        Me.OnDelete = onDelete
    End Sub

    Public Function Script() As String
        Dim text As New StringBuilder()
        text.AppendFormat("ALTER TABLE [{0}].[{1}] WITH CHECK ADD CONSTRAINT [{2}]{3}", _
                          Table.Owner, Table.Name, Name, vbCrLf)
        text.AppendFormat("FOREIGN KEY([{0}]) REFERENCES [{1}].[{2}] ([{3}]){4}", _
                          String.Join("], [", Columns.ToArray()), RefTable.Owner, RefTable.Name, _
                          String.Join("], [", RefColumns.ToArray()), vbCrLf)
        Return text.ToString()
    End Function
End Class
