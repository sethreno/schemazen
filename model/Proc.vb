Public Class Proc
    Public Schema As String
    Public Name As String
    Public Text As String

    Public Sub New(ByVal schema As String, ByVal name As String)
        Me.Schema = schema
        Me.Name = name
    End Sub

    Public Function Script() As String
        Return Text
    End Function

End Class
