Public Class TestHelper
    Public Shared Sub ExecSql(ByVal sql As String)
        Using cn As New SqlClient.SqlConnection(ConfigHelper.TestDB)
            cn.Open()
            Using cm As SqlClient.SqlCommand = cn.CreateCommand()
                cm.CommandText = sql
                cm.ExecuteNonQuery()
            End Using
        End Using
    End Sub
End Class
