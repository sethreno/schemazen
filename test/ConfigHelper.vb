Public Class ConfigHelper
    Public Shared ReadOnly Property TestDB() As String
        Get
            Dim appSettings As New Configuration.AppSettingsReader()
            Return CStr(appSettings.GetValue("testdb", GetType(String)))
        End Get
    End Property

    Public Shared ReadOnly Property TestSchemaDir() As String
        Get
            Dim appSettings As New Configuration.AppSettingsReader()
            Return CStr(appSettings.GetValue("test_schema_dir", GetType(String)))
        End Get
    End Property

    Public Shared ReadOnly Property SqlDbDiffPath() As String
        Get
            Dim appSettings As New Configuration.AppSettingsReader()
            Return CStr(appSettings.GetValue("SqlDbDiffPath", GetType(String)))
        End Get
    End Property

End Class
