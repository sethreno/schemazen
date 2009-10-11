Public Class Table
    Public Sub New(ByVal owner As String, ByVal name As String)
        Me.Owner = owner
        Me.Name = name
    End Sub
    Public Owner As String
    Public Name As String
    Public Columns As New ColumnList
    Public Constraints As New List(Of Constraint)
    Public ForeignKeys As New List(Of ForeignKey)
    Public Triggers As New List(Of Trigger)

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

        Return diff
    End Function

    Public Function Script() As String
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
        Return text.ToString()
    End Function
End Class

Public Class TableDiff
    Public Owner As String
    Public Name As String
    Public ColumnsAdded As New List(Of Column)
    Public ColumnsDroped As New List(Of Column)
    Public ColumnsDiff As New List(Of ColumnDiff)
    Public ReadOnly Property IsDiff() As Boolean
        Get
            Return ColumnsAdded.Count + ColumnsDroped.Count _
                + ColumnsDiff.Count > 0
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
