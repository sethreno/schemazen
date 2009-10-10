Public Class Constraint
    Public Name As String
    Public Type As String
    Public Columns As New List(Of String)

    Public Sub New(ByVal name As String, ByVal type As String, Optional ByVal columns As String = "")
        Me.Name = name
        Me.Type = type
        If Not String.IsNullOrEmpty(columns) Then
            Me.Columns = New List(Of String)(columns.Split(","c))
        End If
    End Sub

    Public Function Script() As String
        Return String.Format("CONSTRAINT [{0}] {1} ([{2}])", _
                             Name, Type, String.Join("], [", Columns.ToArray()))
    End Function
End Class
