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

    Public Shared Sub ExecBatchSql(ByVal sql As String, ByVal dbName As String)
        Using cn As New Data.SqlClient.SqlConnection(GetConnString(dbName))
            cn.Open()
            Using cm As Data.SqlClient.SqlCommand = cn.CreateCommand()
                For Each script As String In sql.Split((vbCrLf + "GO" + vbCrLf).Split(","c), System.StringSplitOptions.RemoveEmptyEntries)
                    Console.WriteLine(script)
                    cm.CommandText = script
                    cm.ExecuteNonQuery()
                Next
            End Using
        End Using
    End Sub

    <Test()> Public Sub TestSplit()
        Dim str As String = "script 1 line 1" + vbCrLf + "script 1 line 2" + vbCrLf + "GO" + vbCrLf + "line 2"
        Dim arr As String() = str.Split("GO".Split(","c), System.StringSplitOptions.None)
        Assert.AreEqual(2, arr.Length)
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
