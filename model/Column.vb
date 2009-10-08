Public Class Column
    Public Name As String
    Public Position As Integer
    Public [Default] As String
    Public IsNullable As Boolean
    Public Type As String
    Public Length As Integer
    Public Precision As Byte
    Public Scale As Integer

    Private ReadOnly Property IsNullableText() As String
        Get
            If IsNullable Then Return "NULL"
            Return "NOT NULL"
        End Get
    End Property

    Public Sub New(ByVal name As String, ByVal type As String, ByVal null As Boolean)
        Me.Name = name
        Me.Type = type
    End Sub

    Public Sub New(ByVal name As String, ByVal type As String, ByVal length As Integer, ByVal null As Boolean)
        Me.New(name, type, null)
        Me.Length = length
    End Sub

    Public Sub New(ByVal name As String, ByVal type As String, ByVal precision As Byte, ByVal scale As Integer, ByVal null As Boolean)
        Me.New(name, type, null)
        Me.Precision = precision
        Me.Scale = scale
    End Sub

    Public Function Compare(ByVal c As Column) As ColumnDiff
        Return New ColumnDiff(Me, c)
    End Function

    Public Function Script() As String
        Select Case Type
            Case "bigint", "bit", "datetime", "float", _
                "image", "int", "money", "ntext", "real", _
                "smalldatetime", "smallint", "smallmoney", _
                "sql_variant", "text", "timestamp", "tinyint", _
                "uniqueidentifier", "xml"
                Return String.Format("[{0}] [{1}] {2}", Name, Type, IsNullableText)

            Case "binary", "char", "nchar", _
                 "nvarchar", "varbinary", "varchar"
                Dim lengthString As String = Length.ToString()
                If lengthString = "-1" Then lengthString = "max"
                Return String.Format("[{0}] [{1}]({2}) {3}", Name, Type, lengthString, IsNullableText)

            Case "decimal", "numeric"
                Return String.Format("[{0}] [{1}]({2},{3}) {4}", Name, Type, Precision, Scale, IsNullableText)

            Case Else
                Throw New NotSupportedException("SQL data type " + Type + " is not supported.")
        End Select
    End Function
End Class

Public Class ColumnDiff
    Public Sub New(ByVal source As Column, ByVal target As Column)
        Me.Source = source
        Me.Target = target
    End Sub
    Public Source As Column
    Public Target As Column
    Public ReadOnly Property IsDiff() As Boolean
        Get
            Return Source.Default <> Target.Default _
              OrElse Source.IsNullable <> Target.IsNullable _
              OrElse Source.Length <> Target.Length _
              OrElse Source.Position <> Target.Position _
              OrElse Source.Type <> Target.Type
        End Get
    End Property

    Public Function Script() As String
        Return Target.Script()
    End Function
End Class
