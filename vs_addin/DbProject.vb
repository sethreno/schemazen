Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Text.RegularExpressions

Public Class DbProject
	Inherits DbFolder

	Public Sub New()
		MyBase.New("")
	End Sub

	Public FileName As String

	Private mDir As String = ""
	Public ReadOnly Property Dir() As String
		Get
			If String.IsNullOrEmpty(mDir) Then
				mDir = New IO.FileInfo(FileName).DirectoryName
			End If
			Return mDir
		End Get
	End Property

	Public DbRefs As New List(Of DbRef)

	Public ReadOnly Property DefDBRef() As DbRef
		Get
			For Each r As DbRef In DbRefs
				If r.Default Then Return r
			Next
			Return Nothing
		End Get
	End Property

	Public Sub Load(ByVal dbProjFilePath As String)
		FileName = dbProjFilePath
		Dim folderStack As New Stack(Of DbFolder)
		Dim parsingDbRefs As Boolean = False
		Dim currentDbRef As DbRef = Nothing
		Dim defaultDbRef As String = ""
		folderStack.Push(Me)

		For Each line As String In IO.File.ReadAllLines(dbProjFilePath)
			If line.Contains("Begin DataProject = ") Then Name = GetValue(line)
			If line.Contains("DefDBRef = ") Then defaultDbRef = GetValue(line)
			If line.Contains("Begin DBRefFolder = ") Then parsingDbRefs = True
			If line.Contains("Begin DBRefNode = ") Then
				currentDbRef = New DbRef(GetValue(line))
				DbRefs.Add(currentDbRef)
			End If

			If currentDbRef IsNot Nothing Then
				If line.Contains("ConnectStr = ") Then currentDbRef.ConnectStr = GetValue(line)
				If line.Contains("Provider = ") Then currentDbRef.Provider = GetValue(line)
				If line.Contains("Colorizer = ") Then
					Integer.TryParse(line.Replace("Colorizer = ", "").Trim(), currentDbRef.Colorizer)
				End If
			Else
				If line.Contains("Begin Folder = ") Then
					Dim f As New DbFolder(GetValue(line))
					folderStack.Peek.Folders.Add(f)
					folderStack.Push(f)
				End If
				If line.Contains("Node = ") Then
					folderStack.Peek.Files.Add(GetValue(line))
				End If
				If line.Contains("Script = ") Then
					folderStack.Peek.Files.Add(GetValue(line))
				End If
			End If

			If line.Trim().ToLower() = "end" Then
				If parsingDbRefs Then
					If currentDbRef IsNot Nothing Then
						'end of DbRefNode
						currentDbRef = Nothing
					Else
						'end of DbRefFolder
						parsingDbRefs = False
					End If
				Else
					'end of folder
					folderStack.Pop()
				End If
			End If
		Next

		For Each r As DbRef In DbRefs
			If r.Name = defaultDbRef Then r.Default = True
		Next
	End Sub

	Public Sub Save()
		IO.File.WriteAllText(FileName, ToString())
	End Sub

	Public Overrides Function ToString() As String
		Dim text As New Text.StringBuilder()
		text.AppendLine("# Microsoft Developer Studio Project File - Database Project")
		text.AppendFormat("Begin DataProject = ""{0}""{1}", Name, vbCrLf)
		text.AppendLine("MSDTVersion = ""80""")
		If DefDBRef IsNot Nothing Then
			text.AppendFormat("   DefDBRef = ""{0}""{1}", DefDBRef.Name, vbCrLf)
		End If
		If DbRefs.Count > 0 Then
			text.AppendLine("   Begin DBRefFolder = ""Database References""")
			For Each d As DbRef In DbRefs
				text.Append(d.ToString())
			Next
			text.AppendLine("   End")
		End If
		text.Append(FileAndFolderText())
		text.AppendLine("   End")
		Return text.ToString()
	End Function

	Private Function GetValue(ByVal line As String)
		Return Regex.Match(line, """(?<val>.*)""").Groups("val").Value
	End Function

End Class

Public Class DbFolder
	Public Sub New(ByVal name As String)
		Me.Name = name
	End Sub
	Public Name As String
	Public Path As String
	Public Files As New List(Of String)
	Public Folders As New List(Of DbFolder)

	Public Function FindFolder(ByVal name As String) As DbFolder
		For Each f As DbFolder In Folders
			If f.Name.ToLower() = name.ToLower() Then Return f
		Next
		Return Nothing
	End Function

	Public Overrides Function ToString() As String
		Dim text As New Text.StringBuilder()
		text.AppendFormat("   Begin Folder = ""{0}""{1}", Name, vbCrLf)
		text.Append(FileAndFolderText())
		text.AppendFormat("   End{0}", vbCrLf)
		Return text.ToString()
	End Function

	Protected Function FileAndFolderText() As String
		Dim text As New Text.StringBuilder()
		For Each f As String In Files
			If f.ToLower().EndsWith(".sql") Then
				text.AppendFormat("      Script = ""{0}""{1}", f, vbCrLf)
			Else
				text.AppendFormat("      Node = ""{0}""{1}", f, vbCrLf)
			End If
		Next
		For Each f As DbFolder In Folders
			text.Append(f.ToString())
		Next
		Return text.ToString()
	End Function

End Class

Public Class DbRef
	Public Sub New(ByVal name As String)
		Me.Name = name
	End Sub
	Public Name As String
	Public ConnectStr As String
	Public Provider As String
	Public Colorizer As Integer
	Public [Default] As Boolean

	Public Overrides Function ToString() As String
		Dim text As New Text.StringBuilder()
		text.AppendFormat("   Begin DBRefNode = ""{0}""{1}", Name, vbCrLf)
		text.AppendFormat("      ConnectStr = ""{0}""{1}", ConnectStr, vbCrLf)
		text.AppendFormat("      Provider = ""{0}""{1}", Provider, vbCrLf)
		text.AppendFormat("      Colorizer = {0}{1}", Colorizer, vbCrLf)
		text.AppendFormat("   End{0}", vbCrLf)
		Return text.ToString()
	End Function
End Class

