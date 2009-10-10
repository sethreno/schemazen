Public Class Identity
    Public Seed As Integer
    Public Increment As Integer

    Public Sub New(ByVal seed As Integer, ByVal increment As Integer)
        Me.Seed = seed
        Me.Increment = increment
    End Sub

    Public Function Script() As String
        Return String.Format("IDENTITY ({0},{1})", Seed, Increment)
    End Function

End Class
