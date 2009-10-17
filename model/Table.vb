Imports System.Data.SqlClient

Public Class Table
    Public Sub New(ByVal owner As String, ByVal name As String)
        Me.Owner = owner
        Me.Name = name
    End Sub
    Public Owner As String
    Public Name As String
    Public Columns As New ColumnList
    Public Constraints As New List(Of Constraint)

    Public Function FindConstraint(ByVal name As String) As Constraint
        For Each c As Constraint In Constraints
            If c.Name = name Then Return c
        Next
        Return Nothing
    End Function

    Public ReadOnly Property PrimaryKey() As Constraint
        Get
            For Each c As Constraint In Constraints
                If c.Type = "PRIMARY KEY" Then Return c
            Next
            Return Nothing
        End Get
    End Property

    Public Function Compare(ByVal t As Table) As TableDiff
        Dim diff As New TableDiff()
        diff.Owner = t.Owner
        diff.Name = t.Name

        'get additions and compare mutual columns
        For Each c As Column In Columns.Items
            Dim c2 As Column = t.Columns.Find(c.Name)
            If c2 Is Nothing Then
                diff.ColumnsAdded.Add(c)
            Else
                'compare mutual columns
                Dim cDiff As ColumnDiff = c.Compare(c2)
                If cDiff.IsDiff Then
                    diff.ColumnsDiff.Add(cDiff)
                End If
            End If
        Next

        'get deletions
        For Each c As Column In t.Columns.Items
            If Columns.Find(c.Name) Is Nothing Then
                diff.ColumnsDroped.Add(c)
            End If
        Next

        'get added and compare mutual constraints
        For Each c As Constraint In Constraints
            Dim c2 As Constraint = t.FindConstraint(c.Name)
            If c2 Is Nothing Then
                diff.ConstraintsAdded.Add(c)
            Else
                If c.Script <> c2.Script Then
                    diff.ConstraintsChanged.Add(c)
                End If
            End If
        Next
        'get deleted constraints
        For Each c As Constraint In t.Constraints
            If FindConstraint(c.Name) Is Nothing Then
                diff.ConstraintsDeleted.Add(c)
            End If
        Next

        Return diff
    End Function

    Public Function ScriptCreate() As String
        Dim text As New StringBuilder()
        text.AppendFormat("CREATE TABLE [{0}].[{1}]({2}", Owner, Name, vbCrLf)
        text.Append(Columns.Script())
        If Constraints.Count > 0 Then text.AppendLine()
        For Each c As Constraint In Constraints
            If c.Type = "INDEX" Then Continue For
            text.AppendLine("   ," + c.Script())
        Next
        text.AppendLine(")")
        text.AppendLine()
        For Each c As Constraint In Constraints
            If c.Type <> "INDEX" Then Continue For
            text.AppendLine(c.Script())
        Next
        Return text.ToString()
    End Function

    Public Function ScriptDrop() As String
        Return String.Format("DROP TABLE [{0}].[{1}]", Owner, Name)
    End Function

    Public Function ExportData(ByVal conn As String) As String
        Dim data As New StringBuilder()
        Dim sql As New StringBuilder()
        sql.Append("select ")
        For Each c As Column In Columns.Items
            sql.AppendFormat("{0},", c.Name)
        Next
        sql.Remove(sql.Length - 1, 1)
        sql.AppendFormat(" from {0}", Name)
        Using cn As New SqlConnection(conn)
            cn.Open()
            Using cm As SqlCommand = cn.CreateCommand()
                cm.CommandText = sql.ToString()
                Using dr As SqlDataReader = cm.ExecuteReader()
                    While dr.Read()
						For Each c As Column In Columns.Items
							data.AppendFormat("{0}{1}", dr(c.Name).ToString(), vbTab)
						Next
                        data.Remove(data.Length - 1, 1)
                        data.AppendLine()
                    End While
                End Using
            End Using
		End Using

        Return data.ToString()
    End Function

    Public Sub ImportData(ByVal conn As String, ByVal data As String)
        Dim dt As New DataTable()
        For Each c As Column In Columns.Items
            dt.Columns.Add(New DataColumn(c.Name))
        Next
        For Each line As String In data.Split(vbCrLf.Split(","c), StringSplitOptions.RemoveEmptyEntries)
            Dim row As DataRow = dt.NewRow()
			Dim fields As String() = line.Split(Chr(9))
			For i As Integer = 0 To fields.Count - 1
				row(i) = ConvertType(Columns.Items(i).Type, fields(i))
			Next
            dt.Rows.Add(row)
        Next
		Dim bulk As New SqlClient.SqlBulkCopy(conn, SqlBulkCopyOptions.KeepIdentity _
											  Or SqlBulkCopyOptions.TableLock)
        bulk.DestinationTableName = Name
        bulk.WriteToServer(dt)
	End Sub

	Public Function ConvertType(ByVal sqlType As String, ByVal val As String) As Object
		If val.Length = 0 Then Return DBNull.Value
		Select Case sqlType.ToLower()
			Case "bit"
				Return Boolean.Parse(val)
			Case "datetime", "smalldatetime"
				Return Date.Parse(val)
			Case Else
				Return val
		End Select
	End Function
End Class

Public Class TableDiff
    Public Owner As String
    Public Name As String

    Public ColumnsAdded As New List(Of Column)
    Public ColumnsDroped As New List(Of Column)
    Public ColumnsDiff As New List(Of ColumnDiff)

    Public ConstraintsAdded As New List(Of Constraint)
    Public ConstraintsChanged As New List(Of Constraint)
    Public ConstraintsDeleted As New List(Of Constraint)

    Public ReadOnly Property IsDiff() As Boolean
        Get
            Return ColumnsAdded.Count + ColumnsDroped.Count + ColumnsDiff.Count _
            + ConstraintsAdded.Count + ConstraintsChanged.Count + ConstraintsDeleted.Count > 0
        End Get
    End Property

    Public Function Script() As String
        Dim text As New StringBuilder()

        For Each c As Column In ColumnsAdded
            text.AppendFormat("ALTER TABLE [{0}].[{1}] ADD {2}", Owner, Name, c.Script())
            text.AppendLine()
        Next

        For Each c As Column In ColumnsDroped
            text.AppendFormat("ALTER TABLE [{0}].[{1}] DROP COLUMN [{2}]", Owner, Name, c.Name)
            text.AppendLine()
        Next

        For Each c As ColumnDiff In ColumnsDiff
            text.AppendFormat("ALTER TABLE [{0}].[{1}] ALTER COLUMN {2}", Owner, Name, c.Script())
            text.AppendLine()
        Next
        text.AppendLine("GO")
        text.AppendLine()

        Return text.ToString()
    End Function
End Class
