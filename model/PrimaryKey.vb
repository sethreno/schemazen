Public Class PrimaryKey
    Public Name As String
    Public Columns As New List(Of String)

    Public Sub New(ByVal name As String, ByVal columns As String)
        Me.Name = name
        Me.Columns = New List(Of String)(columns.Split(","c))
    End Sub

    Public Function Script() As String
        Return String.Format("CONSTRAINT [{0}] PRIMARY KEY ([{1}])", _
                             Name, String.Join("], [", Columns.ToArray()))
    End Function
End Class
