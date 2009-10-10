Public Class Database
    Public Name As String
    Public Tables As New List(Of Table)
    Public Procs As New List(Of Proc)
    Public Functions As New List(Of [Function])

    Public Sub New()

    End Sub

    Public Sub New(ByVal name As String)
        Me.Name = name
    End Sub

    Public Function Script() As String
        Return String.Format("CREATE DATABASE {0}", Name)
    End Function

    Public Function ScriptObjects() As String
        Dim text As New StringBuilder()
        For Each t As Table In Tables
            text.Append(t.Script())
        Next
        For Each t As Table In Tables
            For Each fk As ForeignKey In t.ForeignKeys
                text.AppendLine(fk.Script())
            Next
        Next

        Return Text.ToString()
    End Function

    Public Function FindTable(ByVal name As String) As Table
        For Each t As Table In Tables
            If t.Name = name Then Return t
        Next
        Return Nothing
    End Function

    Public Function FindTableByPk(ByVal name As String) As Table
        For Each t As Table In Tables
            If t.PrimaryKey IsNot Nothing AndAlso t.PrimaryKey.Name = name Then
                Return t
            End If
        Next
        Return Nothing
    End Function

    Public Function FindPrimaryKey(ByVal name As String) As PrimaryKey
        For Each t As Table In Tables
            If t.PrimaryKey IsNot Nothing AndAlso t.PrimaryKey.Name = name Then
                Return t.PrimaryKey
            End If
        Next
        Return Nothing
    End Function

    Public Function FindForeignKey(ByVal name As String) As ForeignKey
        For Each t As Table In Tables
            For Each fk As ForeignKey In t.ForeignKeys
                If fk.Name = name Then Return fk
            Next
        Next
        Return Nothing
    End Function

    Public Sub Load(ByVal connString As String)
        Using cn As New SqlClient.SqlConnection(connString)
            cn.Open()
            Using cm As SqlClient.SqlCommand = cn.CreateCommand()
                'get tables
                cm.CommandText = "select TABLE_SCHEMA, TABLE_NAME from INFORMATION_SCHEMA.TABLES"
                Using dr As SqlClient.SqlDataReader = cm.ExecuteReader()
                    While dr.Read()
                        Tables.Add(New Table(CStr(dr("TABLE_SCHEMA")), CStr(dr("TABLE_NAME"))))
                    End While
                End Using

                'get columns
                cm.CommandText = "select TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE, " _
                    + " CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION," _
                    + " NUMERIC_SCALE from INFORMATION_SCHEMA.COLUMNS"
                Using dr As SqlClient.SqlDataReader = cm.ExecuteReader()
                    While dr.Read()
                        Dim c As New Column
                        c.Name = CStr(dr("COLUMN_NAME"))
                        c.Type = CStr(dr("DATA_TYPE"))
                        c.IsNullable = CStr(dr("IS_NULLABLE")) = "YES"

                        Select Case c.Type
                            Case "binary", "char", "nchar", "nvarchar", "varbinary", "varchar"
                                c.Length = CInt(dr("CHARACTER_MAXIMUM_LENGTH"))
                            Case "decimal", "numeric"
                                c.Precision = CByte(dr("NUMERIC_PRECISION"))
                                c.Scale = CByte(dr("NUMERIC_SCALE"))
                        End Select

                        FindTable(CStr(dr("TABLE_NAME"))).Columns.Add(c)
                    End While
                End Using

                'get primary keys
                cm.CommandText = "select TABLE_NAME, CONSTRAINT_NAME from INFORMATION_SCHEMA.TABLE_CONSTRAINTS" _
                                + " where CONSTRAINT_TYPE = 'PRIMARY KEY'"
                Using dr As SqlClient.SqlDataReader = cm.ExecuteReader()
                    While dr.Read()
                        FindTable(CStr(dr("TABLE_NAME"))).PrimaryKey = New PrimaryKey(CStr(dr("CONSTRAINT_NAME")))
                    End While
                End Using

                'get primarykey columns
                cm.CommandText = "select CONSTRAINT_NAME, COLUMN_NAME from INFORMATION_SCHEMA.KEY_COLUMN_USAGE"
                Using dr As SqlClient.SqlDataReader = cm.ExecuteReader()
                    While dr.Read()
                        Dim pk As PrimaryKey = FindPrimaryKey(CStr(dr("CONSTRAINT_NAME")))
                        If pk IsNot Nothing Then pk.Columns.Add(CStr(dr("COLUMN_NAME")))
                    End While
                End Using

                'get foreign keys
                cm.CommandText = "select TABLE_NAME, CONSTRAINT_NAME from INFORMATION_SCHEMA.TABLE_CONSTRAINTS" _
                                + " where CONSTRAINT_TYPE = 'FOREIGN KEY'"
                Using dr As SqlClient.SqlDataReader = cm.ExecuteReader()
                    While dr.Read()
                        Dim t As Table = FindTable(CStr(dr("TABLE_NAME")))
                        Dim fk As New ForeignKey(CStr(dr("CONSTRAINT_NAME")))
                        fk.Table = t
                        t.ForeignKeys.Add(fk)
                    End While
                End Using

                'get foreign key props
                cm.CommandText = "select CONSTRAINT_NAME, UNIQUE_CONSTRAINT_NAME, UPDATE_RULE, DELETE_RULE" _
                                + " from INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS"
                Using dr As SqlClient.SqlDataReader = cm.ExecuteReader()
                    While dr.Read()
                        Dim fk As ForeignKey = FindForeignKey(CStr(dr("CONSTRAINT_NAME")))
                        fk.RefTable = FindTableByPk(CStr(dr("UNIQUE_CONSTRAINT_NAME")))
                        For Each c As Column In fk.RefTable.Columns.Items
                            fk.RefColumns.Add(c.Name)
                        Next
                        fk.OnUpdate = CStr(dr("UPDATE_RULE"))
                        fk.OnDelete = CStr(dr("DELETE_RULE"))
                    End While
                End Using

                'get foreign key columns
                cm.CommandText = "select CONSTRAINT_NAME, COLUMN_NAME from INFORMATION_SCHEMA.KEY_COLUMN_USAGE"
                Using dr As SqlClient.SqlDataReader = cm.ExecuteReader()
                    While dr.Read()
                        Dim fk As ForeignKey = FindForeignKey(CStr(dr("CONSTRAINT_NAME")))
                        If fk IsNot Nothing Then fk.Columns.Add(CStr(dr("COLUMN_NAME")))
                    End While
                End Using
            End Using
        End Using
    End Sub
End Class
