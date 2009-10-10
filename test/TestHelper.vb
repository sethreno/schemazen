Public Class TestHelper
    Public Shared Sub ExecSql(ByVal sql As String, Optional ByVal dbName As String = "")
        Console.WriteLine(sql)
        Using cn As New Data.SqlClient.SqlConnection(ConfigHelper.TestDB)
            If Not String.IsNullOrEmpty(dbName) Then
                cn.ConnectionString = GetConnString(dbName)
            End If
            cn.Open()
            Using cm As Data.SqlClient.SqlCommand = cn.CreateCommand()
                cm.CommandText = sql
                cm.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Public Shared Function GetConnString(ByVal dbName As String) As String
        Dim connString As String = ""
        Using cn As New Data.SqlClient.SqlConnection(ConfigHelper.TestDB)
            connString = cn.ConnectionString.Replace("database=" + cn.Database, "database=" + dbName)
        End Using
        Return connString
    End Function

    Public Shared Sub DropDb(ByVal dbName As String)
        If DbExists(dbName) Then
            ExecSql("ALTER DATABASE " + dbName + " SET SINGLE_USER WITH ROLLBACK IMMEDIATE")
            ExecSql("drop database " + dbName)
        End If
    End Sub

    Public Shared Function DbExists(ByVal dbName As String) As Boolean
        Dim exists As Boolean
        Using cn As New Data.SqlClient.SqlConnection(ConfigHelper.TestDB)
            cn.Open()
            Using cm As Data.SqlClient.SqlCommand = cn.CreateCommand()
                cm.CommandText = "select db_id('" + dbName + "')"
                exists = Not cm.ExecuteScalar() Is DBNull.Value
            End Using
        End Using

        Return exists
    End Function

End Class
