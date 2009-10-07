Imports System.Collections.ObjectModel

Public Class ColumnList

    Private mItems As New List(Of Column)
    Public ReadOnly Property Items() As ReadOnlyCollection(Of Column)
        Get
            Return mItems.AsReadOnly()
        End Get
    End Property

    Public Sub Add(ByVal c As Column)
        mItems.Add(c)
    End Sub

    Public Sub Remove(ByVal c As Column)
        mItems.Remove(c)
    End Sub

    Public Function Find(ByVal name As String) As Column
        For Each c As Column In mItems
            If c.Name = name Then Return c
        Next
        Return Nothing
    End Function

    Public Function Script() As String
        Dim text As New Text.StringBuilder()
        For Each c As Column In mItems
            text.Append("   " + c.Script())
            If mItems.IndexOf(c) < mItems.Count - 1 Then
                text.AppendLine(",")
            Else
                text.AppendLine()
            End If
        Next
        Return text.ToString()
    End Function
End Class
