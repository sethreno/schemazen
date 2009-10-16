Imports System.Configuration

Public Class ConfigHelper
    Public Shared Function GetConnectionString(ByVal name As String) As String
        Return ConfigurationManager.ConnectionStrings(name).ConnectionString
    End Function

    Public Shared Function GetAppSetting(ByVal key As String) As String
        Return ConfigurationManager.AppSettings(key)
    End Function

End Class
