Public Class TestHelper
    Public Shared Sub ExecSql(ByVal sql As String, Optional ByVal dbName As String = "")
        Console.WriteLine(sql)
        Using cn As New SqlClient.SqlConnection(ConfigHelper.TestDB)
            If Not String.IsNullOrEmpty(dbName) Then
                cn.ConnectionString = GetConnString(dbName)
            End If
            cn.Open()
            Using cm As SqlClient.SqlCommand = cn.CreateCommand()
                cm.CommandText = sql
                cm.ExecuteNonQuery()
            End Using
            cn.Close()
        End Using
    End Sub

    Public Shared Function GetConnString(ByVal dbName As String) As String
        Dim connString As String = ""
        Using cn As New SqlClient.SqlConnection(ConfigHelper.TestDB)
            connString = cn.ConnectionString.Replace("database=" + cn.Database, "database=" + dbName)
        End Using
        Return connString
    End Function


End Class
