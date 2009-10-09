Public Class ConfigHelper
    Public Shared ReadOnly Property TestDB() As String
        Get
            Dim appSettings As New Configuration.AppSettingsReader()
            Return CStr(appSettings.GetValue("testdb", GetType(String)))
        End Get
    End Property

End Class
