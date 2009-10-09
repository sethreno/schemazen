Public Class Column
    Public Name As String
    Public Position As Integer
    Public [Default] As [Default]
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

    Public ReadOnly Property DefaultText() As String
        Get
            If Me.Default Is Nothing Then Return ""
            Return Me.Default.Script()
        End Get
    End Property

    Public Sub New(ByVal name As String, ByVal type As String, ByVal null As Boolean, Optional ByVal [default] As [Default] = Nothing)
        Me.Name = name
        Me.Type = type
        Me.Default = [default]
    End Sub

    Public Sub New(ByVal name As String, ByVal type As String, ByVal length As Integer, ByVal null As Boolean, Optional ByVal [default] As [Default] = Nothing)
        Me.New(name, type, null, [default])
        Me.Length = length
    End Sub

    Public Sub New(ByVal name As String, ByVal type As String, ByVal precision As Byte, ByVal scale As Integer, ByVal null As Boolean, Optional ByVal [default] As [Default] = Nothing)
        Me.New(name, type, null, [Default])
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
                Return String.Format("[{0}] [{1}] {2} {3}", Name, Type, IsNullableText, DefaultText)

            Case "binary", "char", "nchar", _
                 "nvarchar", "varbinary", "varchar"
                Dim lengthString As String = Length.ToString()
                If lengthString = "-1" Then lengthString = "max"
                Return String.Format("[{0}] [{1}]({2}) {3} {4}", Name, Type, lengthString, IsNullableText, DefaultText)

            Case "decimal", "numeric"
                Return String.Format("[{0}] [{1}]({2},{3}) {4} {5}", Name, Type, Precision, Scale, IsNullableText, DefaultText)

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
            Return Source.DefaultText <> Target.DefaultText _
              OrElse Source.IsNullable <> Target.IsNullable _
              OrElse Source.Length <> Target.Length _
              OrElse Source.Position <> Target.Position _
              OrElse Source.Type <> Target.Type _
              OrElse Source.Precision <> Target.Precision _
              OrElse Source.Scale <> Target.Scale
        End Get
    End Property

    Public Function Script() As String
        Return Target.Script()
    End Function
End Class
