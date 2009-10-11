Public Class Constraint
    Public Table As Table
    Public Name As String
    Public Type As String
    Public Clustered As Boolean
    Public Columns As New List(Of String)

    Public ReadOnly Property ClusteredText() As String
        Get
            If Not Clustered Then Return ""
            Return "CLUSTERED"
        End Get
    End Property

    Public Sub New(ByVal name As String, ByVal type As String, Optional ByVal columns As String = "")
        Me.Name = name
        Me.Type = type
        If Not String.IsNullOrEmpty(columns) Then
            Me.Columns = New List(Of String)(columns.Split(","c))
        End If
    End Sub

    Public Function Script() As String
        If Type = "INDEX" Then
            Return String.Format("CREATE {0} INDEX [{1}] ON [{2}].[{3}] ([{4}])", _
                   ClusteredText, Name, Table.Owner, Table.Name, String.Join("], [", Columns.ToArray()))
        End If
        Return String.Format("CONSTRAINT [{0}] {1} {2} ([{3}])", _
                            Name, Type, ClusteredText, String.Join("], [", Columns.ToArray()))
    End Function
End Class
