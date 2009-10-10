<TestFixture()> _
Public Class FunctionTester

    <Test()> Public Sub TestScript()
        Dim f As New [Function]("dbo", "udf_GetDate")
        f.Text = _
        "CREATE FUNCTION [dbo].[udf_GetDate]()" + vbCrLf _
        + "RETURNS DATETIME AS" + vbCrLf _
        + "BEGIN" + vbCrLf _
        + "   RETURN GETDATE()" + vbCrLf _
        + "END" + vbCrLf

        Console.WriteLine(f.Script())
        TestHelper.ExecSql(f.Script())
        TestHelper.ExecSql("drop function [dbo].[udf_GetDate]")
    End Sub

End Class
