Public Class [Default]
    Public Name As String
    Public Value As String

    Public Sub New(ByVal name As String, ByVal value As String)
        Me.Name = name
        Me.Value = value
    End Sub

    Public Function Script() As String
        Return String.Format("CONSTRAINT {0} DEFAULT {1}", Name, Value)
    End Function
End Class
