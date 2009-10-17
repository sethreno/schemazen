<TestFixture()> _
Public Class DBHelperTester

	<Test()> Public Sub TestSplitGONoEndLine()
		Dim scripts As String()
		scripts = DBHelper.SplitBatchSql( _
		"1:1" + vbCrLf + _
		"1:2" + vbCrLf + _
		"GO" _
		)
		'should be 1 script with no 'GO'
		Assert.AreEqual(1, scripts.Count)
		Assert.IsFalse(scripts(0).Contains("GO"))
	End Sub

	<Test()> Public Sub TestSplitGOInComment()
		Dim scripts As String()
		scripts = DBHelper.SplitBatchSql( _
		"1:1" + vbCrLf + _
		"-- GO eff yourself" + vbCrLf + _
		"1:2" + vbCrLf)
		'shoud be 1 script
		Assert.AreEqual(1, scripts.Count)
	End Sub

	<Test()> Public Sub TestSplitGOInQuotes()
		Dim scripts As String()
		scripts = DBHelper.SplitBatchSql( _
		"1:1 ' " + vbCrLf + _
		"GO" + vbCrLf + _
		"' 1:2" + vbCrLf)
		'should be 1 script
		Assert.AreEqual(1, scripts.Count)
	End Sub

	<Test()> Public Sub TestSplitMultipleGOs()
		Dim scripts As String()
		scripts = DBHelper.SplitBatchSql( _
		"1:1" + vbCrLf + _
		"GO" + vbCrLf + _
		"GO" + vbCrLf + _
		"GO" + vbCrLf + _
		"GO" + vbCrLf + _
		"2:1")
		'should be 2 scripts
		Assert.AreEqual(2, scripts.Count)
	End Sub


End Class
