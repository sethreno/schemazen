Public Class Database

#Region " Constructors "

    Public Sub New()

    End Sub

    Public Sub New(ByVal name As String)
        Me.Name = name
    End Sub

#End Region

#Region " Properties "

    Public Name As String
    Public Tables As New List(Of Table)
    Public Routines As New List(Of Routine)
    Public ForeignKeys As New List(Of ForeignKey)

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

    Public Function FindConstraint(ByVal name As String) As Constraint
        For Each t As Table In Tables
            For Each c As Constraint In t.Constraints
                If c.Name = name Then Return c
            Next
        Next
        Return Nothing
    End Function

    Public Function FindForeignKey(ByVal name As String) As ForeignKey
        For Each fk As ForeignKey In ForeignKeys
            If fk.Name = name Then Return fk
        Next
        Return Nothing
    End Function

    Public Function FindRoutine(ByVal name As String) As Routine
        For Each r As Routine In Routines
            If r.Name = name Then Return r
        Next
        Return Nothing
    End Function

#End Region

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

                'get column identities
                cm.CommandText = "select t.name as TABLE_NAME, c.name AS COLUMN_NAME, i.SEED_VALUE, i.INCREMENT_VALUE" _
                    + " from sys.tables t inner join sys.columns c on c.object_id = t.object_id" _
                    + " inner join sys.identity_columns i on i.object_id = c.object_id" _
                    + " and i.column_id = c.column_id"
                Using dr As SqlClient.SqlDataReader = cm.ExecuteReader()
                    While dr.Read()
                        Dim t As Table = FindTable(CStr(dr("TABLE_NAME")))
                        t.Columns.Find(CStr(dr("COLUMN_NAME"))).Identity = _
                            New Identity(CInt(dr("SEED_VALUE")), CInt(dr("INCREMENT_VALUE")))
                    End While
                End Using

                'get column defaults
                cm.CommandText = "select t.name as TABLE_NAME, c.name as COLUMN_NAME, d.name as DEFAULT_NAME, d.definition as DEFAULT_VALUE" _
                                + " from sys.tables t inner join sys.columns c on c.object_id = t.object_id" _
                                + "	inner join sys.default_constraints d on c.column_id = d.parent_column_id" _
                                + " and d.parent_object_id = c.object_id"
                Using dr As SqlClient.SqlDataReader = cm.ExecuteReader()
                    While dr.Read()
                        Dim t As Table = FindTable(CStr(dr("TABLE_NAME")))
                        t.Columns.Find(CStr(dr("COLUMN_NAME"))).Default = _
                            New [Default](CStr(dr("DEFAULT_NAME")), CStr(dr("DEFAULT_VALUE")))
                    End While
                End Using

                'get constraints & indexes
                cm.CommandText = _
                "select t.name as tableName, i.name as indexName, c.name as columnName," _
                + " i.is_primary_key, i.is_unique_constraint, i.type_desc" _
                + " from sys.tables t " _
                + "	inner join sys.indexes i on i.object_id = t.object_id" _
                + "	inner join sys.index_columns ic on ic.object_id = t.object_id" _
                + "		and ic.index_id = i.index_id" _
                + "	inner join sys.columns c on c.object_id = t.object_id" _
                + "		and c.column_id = ic.column_id" _
                + " order by t.name, i.name, ic.key_ordinal"
                Using dr As SqlClient.SqlDataReader = cm.ExecuteReader()
                    While dr.Read
                        Dim c As Constraint = FindConstraint(CStr(dr("indexName")))
                        If c Is Nothing Then
                            c = New Constraint(CStr(dr("indexName")), "")
                            Dim t As Table = FindTable(CStr(dr("tableName")))
                            t.Constraints.Add(c)
                            c.Table = t
                        End If
                        c.Clustered = CStr(dr("type_desc")) = "CLUSTERED"
                        c.Columns.Add(CStr(dr("columnName")))
                        c.Type = "INDEX"
                        If CBool(dr("is_primary_key")) Then c.Type = "PRIMARY KEY"
                        If CBool(dr("is_unique_constraint")) Then c.Type = "UNIQUE"
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
                        ForeignKeys.Add(fk)
                    End While
                End Using

                'get foreign key props
                cm.CommandText = "select CONSTRAINT_NAME, UNIQUE_CONSTRAINT_NAME, UPDATE_RULE, DELETE_RULE" _
                                + " from INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS"
                Using dr As SqlClient.SqlDataReader = cm.ExecuteReader()
                    While dr.Read()
                        Dim fk As ForeignKey = FindForeignKey(CStr(dr("CONSTRAINT_NAME")))
                        fk.RefTable = FindTableByPk(CStr(dr("UNIQUE_CONSTRAINT_NAME")))
                        fk.RefColumns = fk.RefTable.PrimaryKey.Columns
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

                'get routines
                cm.CommandText = _
                "   select " _
                + "     s.name as schemaName," _
                + "     o.name as routineName, " _
                + "     o.type_desc," _
                + "     m.definition," _
                + "     t.name as tableName" _
                + "	from sys.sql_modules m" _
                + "		inner join sys.objects o on m.object_id = o.object_id" _
                + "		inner join sys.schemas s on s.schema_id = o.schema_id" _
                + "		left join sys.triggers tr on m.object_id = tr.object_id" _
                + "		left join sys.tables t on tr.parent_id = t.object_id"
                Using dr As SqlClient.SqlDataReader = cm.ExecuteReader()
                    While dr.Read()
                        Dim r As New Routine(CStr(dr("schemaName")), CStr(dr("routineName")))
                        r.Text = CStr(dr("definition"))
                        Routines.Add(r)

                        Select Case CStr(dr("type_desc"))
                            Case "SQL_STORED_PROCEDURE"
                                r.Type = "PROCEDURE"
                            Case "SQL_TRIGGER"
                                r.Type = "TRIGGER"
                            Case "SQL_SCALAR_FUNCTION"
                                r.Type = "FUNCTION"
                        End Select
                    End While
                End Using
            End Using
        End Using
    End Sub

    Public Function Compare(ByVal db As Database) As DatabaseDiff
        Dim diff As New DatabaseDiff()

        'get tables added and changed
        For Each t As Table In Tables
            Dim t2 As Table = db.FindTable(t.Name)
            If t2 Is Nothing Then
                diff.TablesAdded.Add(t)
            Else
                'compare mutual tables
                Dim tDiff As TableDiff = t.Compare(t2)
                If tDiff.IsDiff Then
                    diff.TablesDiff.Add(tDiff)
                End If
            End If
        Next
        'get deleted tables
        For Each t As Table In db.Tables
            If FindTable(t.Name) Is Nothing Then
                diff.TablesDeleted.Add(t)
            End If
        Next

        'get procs added and changed
        For Each r As Routine In Routines
            Dim r2 As Routine = db.FindRoutine(r.Name)
            If r2 Is Nothing Then
                diff.RoutinesAdded.Add(r)
            Else
                'compare mutual procs
                If r.Text <> r2.Text Then
                    diff.RoutinesDiff.Add(r)
                End If
            End If
        Next
        'get procs deleted
        For Each r As Routine In db.Routines
            If FindRoutine(r.Name) Is Nothing Then
                diff.RoutinesDeleted.Add(r)
            End If
        Next

        'get added and compare mutual foreign keys
        For Each fk As ForeignKey In ForeignKeys
            Dim fk2 As ForeignKey = db.FindForeignKey(fk.Name)
            If fk2 Is Nothing Then
                diff.ForeignKeysAdded.Add(fk)
            Else
                If fk.ScriptCreate <> fk2.ScriptCreate Then
                    diff.ForeignKeysChanged.Add(fk)
                End If
            End If
        Next
        'get deleted foreign keys
        For Each fk As ForeignKey In db.ForeignKeys
            If FindForeignKey(fk.Name) Is Nothing Then
                diff.ForeignKeysDeleted.Add(fk)
            End If
        Next

        Return diff
    End Function

    Public Function ScriptCreate() As String
        Dim text As New StringBuilder()

        text.AppendFormat("CREATE DATABASE {0}", Name)
        text.AppendLine()
        text.AppendLine("GO")
        text.AppendFormat("USE {0}", Name)
        text.AppendLine()
        text.AppendLine("GO")
        text.AppendLine()

        For Each t As Table In Tables
            text.AppendLine(t.ScriptCreate())
        Next
        text.AppendLine()
        text.AppendLine("GO")

        For Each fk As ForeignKey In ForeignKeys
            text.AppendLine(fk.ScriptCreate())
        Next
        text.AppendLine()
        text.AppendLine("GO")

        For Each r As Routine In Routines
            text.AppendLine(r.ScriptCreate())
            text.AppendLine()
            text.AppendLine("GO")
        Next

        Return text.ToString()
    End Function
End Class

Public Class DatabaseDiff
    Public TablesAdded As New List(Of Table)
    Public TablesDiff As New List(Of TableDiff)
    Public TablesDeleted As New List(Of Table)

    Public RoutinesAdded As New List(Of Routine)
    Public RoutinesDiff As New List(Of Routine)
    Public RoutinesDeleted As New List(Of Routine)

    Public ForeignKeysAdded As New List(Of ForeignKey)
    Public ForeignKeysChanged As New List(Of ForeignKey)
    Public ForeignKeysDeleted As New List(Of ForeignKey)

    Public Function Script() As String
        Dim text As New StringBuilder()
        'delete foreign keys
        For Each fk As ForeignKey In ForeignKeysDeleted
            text.AppendLine(fk.ScriptDrop())
            text.AppendLine()
        Next
        'delete modified foreign keys
        For Each fk As ForeignKey In ForeignKeysChanged
            text.AppendLine(fk.ScriptDrop())
            text.AppendLine()
        Next
        text.AppendLine("GO")

        'add tables
        For Each t As Table In TablesAdded
            text.AppendLine(t.ScriptCreate())
            text.AppendLine()
        Next
        text.AppendLine("GO")

        'modify tables
        For Each t As TableDiff In TablesDiff
            text.AppendLine(t.Script())
        Next
        text.AppendLine("GO")

        'delete tables
        For Each t As Table In TablesDeleted
            text.AppendLine(t.ScriptDrop())
        Next
        text.AppendLine("GO")

        'add foreign keys
        For Each fk As ForeignKey In ForeignKeysAdded
            text.AppendLine(fk.ScriptCreate())
        Next
        'add modified foreign keys
        For Each fk As ForeignKey In ForeignKeysChanged
            text.AppendLine(fk.ScriptCreate())
        Next
        text.AppendLine("GO")

        'add & delete procs, functions, & triggers
        For Each r As Routine In RoutinesAdded
            text.AppendLine(r.ScriptCreate())
            text.AppendLine("GO")
        Next
        For Each r As Routine In RoutinesDiff
            text.AppendLine(r.ScriptDrop())
            text.AppendLine("GO")
            text.AppendLine(r.ScriptCreate())
            text.AppendLine("GO")
        Next

        Return text.ToString()
    End Function
End Class