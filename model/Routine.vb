Public Class Routine
    Public Schema As String
    Public Name As String
    Public Text As String
    Public Type As String

    Public Sub New(ByVal schema As String, ByVal name As String)
        Me.Schema = schema
        Me.Name = name
    End Sub

    Public Function ScriptCreate() As String
        Return Text
    End Function

    Public Function ScriptDrop() As String
        Return String.Format("DROP {0} [{1}].[{2}]", Type, Schema, Name)
    End Function

End Class
