Public Class DBHelper
    Public Shared EchoSql As Boolean = False
    
    Public Shared Sub ExecSql(ByVal conn As String, ByVal sql As String)
        If EchoSql Then Console.WriteLine(sql)
        Using cn As New Data.SqlClient.SqlConnection(conn)
            cn.Open()
            Using cm As Data.SqlClient.SqlCommand = cn.CreateCommand()
                cm.CommandText = sql
                cm.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Public Shared Sub ExecBatchSql(ByVal conn As String, ByVal sql As String)
        Using cn As New Data.SqlClient.SqlConnection(conn)
            cn.Open()
            Using cm As Data.SqlClient.SqlCommand = cn.CreateCommand()
                For Each script As String In sql.Split((vbLf + "GO" + vbCr).Split(","c), System.StringSplitOptions.RemoveEmptyEntries)
                    If script.Trim().Replace(vbCrLf, "").ToUpper() = "GO" Then Continue For
                    If EchoSql Then Console.WriteLine(script)
                    cm.CommandText = script
                    cm.ExecuteNonQuery()
                Next
            End Using
        End Using
    End Sub

    Public Shared Sub DropDb(ByVal conn As String)
        Dim cnBuilder As New SqlClient.SqlConnectionStringBuilder(conn)
        Dim dbName As String = cnBuilder.InitialCatalog
        cnBuilder.InitialCatalog = "master"
        If DbExists(cnBuilder.ToString()) Then
            ExecSql(cnBuilder.ToString(), "ALTER DATABASE " + dbName + " SET SINGLE_USER WITH ROLLBACK IMMEDIATE")
            ExecSql(cnBuilder.ToString(), "drop database " + dbName)

            cnBuilder.InitialCatalog = dbName
            ClearPool(cnBuilder.ToString())
        End If
    End Sub

    Public Shared Sub CreateDb(ByVal conn As String)
        Dim cnBuilder As New SqlClient.SqlConnectionStringBuilder(conn)
        Dim dbName As String = cnBuilder.InitialCatalog
        cnBuilder.InitialCatalog = "master"
        ExecSql(cnBuilder.ToString(), "CREATE DATABASE " + dbName)
    End Sub

    Public Shared Function DbExists(ByVal conn As String) As Boolean
        Dim exists As Boolean
        Dim cnBuilder As New SqlClient.SqlConnectionStringBuilder(conn)
        Dim dbName As String = cnBuilder.InitialCatalog
        cnBuilder.InitialCatalog = "master"

        Using cn As New Data.SqlClient.SqlConnection(cnBuilder.ToString)
            cn.Open()
            Using cm As Data.SqlClient.SqlCommand = cn.CreateCommand()
                cm.CommandText = "select db_id('" + dbName + "')"
                exists = Not cm.ExecuteScalar() Is DBNull.Value
            End Using
        End Using

        Return exists
    End Function

    Public Shared Sub ClearPool(ByVal conn As String)
        Using cn As New Data.SqlClient.SqlConnection(conn)
            Data.SqlClient.SqlConnection.ClearPool(cn)
        End Using
    End Sub

End Class
