Imports System.Windows.Forms
Imports System.Collections.Generic

Public Class ChooseLabel

	Public Items As New List(Of String)
	Private mSelectedItem As String
	Public ReadOnly Property SelectedItem() As String
		Get
			Return mSelectedItem
		End Get
	End Property

	Private Sub OK_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OK_Button.Click
		Me.DialogResult = System.Windows.Forms.DialogResult.OK
		mSelectedItem = ListBox1.SelectedValue
		Me.Close()
	End Sub

	Private Sub Cancel_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Cancel_Button.Click
		Me.DialogResult = System.Windows.Forms.DialogResult.Cancel
		Me.Close()
	End Sub

	Private Sub ChooseLabel_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
		ListBox1.DataSource = Items
	End Sub
End Class
