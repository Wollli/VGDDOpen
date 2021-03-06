﻿Imports System.Windows.Forms
Imports System.Drawing
Imports System.IO
Imports VGDDCommon
Imports VGDD

'Imports VGDD.VGDDPlayerLib

Public Class frmPlayer

    Public CurrentScreen As VGDDScreen
    Public Event HistoryBack As EventHandler
    Public Event HystoryForward As EventHandler
    Public Event UserSelectedScreen As EventHandler ' (ByVal ScreenName As String)
    Public UserSelectedScreen_Screen As String
    Private ScreenShotPath As String = Nothing

    Private allowCoolMove As Boolean = False
    Private myCoolPoint As New Point
    Private PanelMoved As Boolean = False
    Private BorderWidth As Integer
    Private TitlebarHeight As Integer
    Public bmpLastScreen As Bitmap

    Private Sub frmPlayer_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        For Each ScreenName As String In Common.aScreens.Keys
            Me.ToolStripdrpScreens.DropDownItems.Add(Common.aScreens(ScreenName).Name)
        Next
        If Common.ProjectMultiLanguageTranslations = 0 Then
            ToolStripdrpLanguage.Visible = False
        Else
            Me.ToolStripdrpLanguage.DropDownItems.Add("Reference Language")
            For i As Integer = 1 To Common.ProjectMultiLanguageTranslations
                Me.ToolStripdrpLanguage.DropDownItems.Add("Translation " & i)
            Next
        End If

        If Common.ProjectPlayerBgBitmapName <> "" Then
            Dim oBgBitmap As VGDDImage = Common.GetBitmap(Common.ProjectPlayerBgBitmapName)
            If oBgBitmap IsNot Nothing Then
                Me.BackgroundImage = oBgBitmap.Bitmap
                Me.BackgroundImageLayout = Windows.Forms.ImageLayout.None
                Me.ClientSize = New Size(Me.BackgroundImage.Width, Me.BackgroundImage.Height + Me.ToolStrip1.Height)
            End If
        End If
        Me.BackColor = Common.ProjectPlayerBgColour
        ResizeMe()
    End Sub

    Public Sub ResizeMe()
        If CurrentScreen IsNot Nothing Then
            'Me.SuspendLayout()
            If Not PanelMoved Then
                With Panel1
                    .Width = CurrentScreen.Width
                    .Height = CurrentScreen.Height
                    If .Height + Me.ToolStrip1.Height + 100 > Screen.PrimaryScreen.WorkingArea.Height Then
                        .Height = Screen.PrimaryScreen.WorkingArea.Height - Me.ToolStrip1.Height - 100
                    End If
                    If .Width + 100 > Screen.PrimaryScreen.WorkingArea.Width Then
                        .Width = Screen.PrimaryScreen.WorkingArea.Width - 100
                    End If
                    If Me.ClientRectangle.Width > .Width Then
                        .Left = (Me.ClientRectangle.Width - .Width) / 2
                    Else
                        .Left = 0
                        Me.Width = .Width
                    End If
                    If Me.ClientSize.Height > .Height + ToolStrip1.Height + 1 Then
                        .Top = (Me.ClientSize.Height - ToolStrip1.Height - .Height) / 2
                    Else 'If Me.Height < .Height + ToolStrip1.Height Then
                        .Top = 0
                        Me.Height = Me.Height - Me.ClientSize.Height + .Height + ToolStrip1.Height + 1
                    End If
                End With
            End If
            'Me.ResumeLayout()
        End If
    End Sub

    Private Sub ToolStripAnimation_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripAnimation.Click
        Common.AnimationEnable = Not Common.AnimationEnable
    End Sub

    Public Sub CheckHistory()
        ToolStripForward.Enabled = (Player.ScreenHistoryIdx < Player.ScreenHistory.Count)
        ToolStripBack.Enabled = (Player.ScreenHistoryIdx > 1)
    End Sub

    Private Sub frmPlayer_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Resize
        ResizeMe()
    End Sub

    Private Sub ToolStripBack_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripBack.Click
        RaiseEvent HistoryBack(Nothing, Nothing)
    End Sub

    Private Sub ToolStripForward_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripForward.Click
        RaiseEvent HystoryForward(Nothing, Nothing)
    End Sub

    Private Sub ToolStripdrpLanguage_DropDownItemClicked(ByVal sender As Object, ByVal e As System.Windows.Forms.ToolStripItemClickedEventArgs) Handles ToolStripdrpLanguage.DropDownItemClicked
        Common.ProjectActiveLanguage = ToolStripdrpLanguage.DropDownItems.IndexOf(e.ClickedItem)
        Panel1.Refresh()
    End Sub

    Private Sub ToolStripdrpScreens_DropDownItemClicked(ByVal sender As Object, ByVal e As System.Windows.Forms.ToolStripItemClickedEventArgs) Handles ToolStripdrpScreens.DropDownItemClicked
        UserSelectedScreen_Screen = e.ClickedItem.Text
        ToolStripdrpScreens.DropDown.Close()
        RaiseEvent UserSelectedScreen(Nothing, Nothing)
    End Sub

    Private Sub ToolStripHelp_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripHelp.Click
        VGDDCommon.Common.RunBrowser("http://virtualfab.it/mediawiki/index.php/VGDD Player")
    End Sub

    Public Function TakeScreenshot(ByVal blnOnlyPanel As Boolean) As Bitmap
        Dim g As Graphics = Me.CreateGraphics()
        Dim hBitmap As Bitmap = Nothing
        Try
            Dim sz As Size = Size
            If blnOnlyPanel Then
                sz = New Size(Me.Panel1.ClientSize.Width, Me.Panel1.ClientSize.Height)
            Else
                sz = New Size(Me.ClientSize.Width, Me.ClientSize.Height - ToolStrip1.Height)
            End If
            hBitmap = New Bitmap(sz.Width + 2, sz.Height + 2, g)
            Dim hGraphics As Graphics = Graphics.FromImage(hBitmap)
            hGraphics.DrawRectangle(New Pen(Brushes.Black), 0, 0, hBitmap.Width - 1, hBitmap.Height - 1)
            If blnOnlyPanel Then
                hGraphics.CopyFromScreen(Me.Left + Me.Panel1.Left + BorderWidth, Me.Top + Me.Panel1.Top + TitlebarHeight + BorderWidth, 1, 1, sz)
            Else
                hGraphics.CopyFromScreen(Me.Left + BorderWidth, Me.Top + TitlebarHeight + BorderWidth, 0, 0, sz)
            End If
        Catch
            If (Not g Is Nothing) Then g.Dispose()
        Finally
            If (Not g Is Nothing) Then g.Dispose()
        End Try
        Return hBitmap
    End Function

    Private Sub ToolStripSnapshot_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripSnapshot.Click
        Dim g As Graphics = Me.CreateGraphics()
        Try
            Dim hBitmap As Bitmap = TakeScreenshot(True)
            If ScreenShotPath Is Nothing Then
                Dim dlg As New FolderBrowserDialog
                dlg.Description = "Choose ScreenShot folder"
                dlg.RootFolder = Environment.SpecialFolder.MyComputer
                dlg.ShowNewFolderButton = True
                If dlg.ShowDialog <> DialogResult.OK Then
                    Exit Sub
                End If
                ScreenShotPath = dlg.SelectedPath
            End If
            Dim strScreenShotFile As String = Path.Combine(ScreenShotPath, CurrentScreen.Name)
            Dim i As Integer = 1
            Do While File.Exists(strScreenShotFile & i.ToString & ".png")
                i += 1
            Loop
            strScreenShotFile = strScreenShotFile & i.ToString & ".png"
            hBitmap.Save(strScreenShotFile, Imaging.ImageFormat.Png)
            MessageBox.Show("Screenshot saved to " & strScreenShotFile, "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch
            If (Not g Is Nothing) Then g.Dispose()
        Finally
            If (Not g Is Nothing) Then g.Dispose()
        End Try
    End Sub

    Private Sub Panel1_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Panel1.MouseDown
        allowCoolMove = True
        myCoolPoint = New Point(e.X, e.Y)
        Me.Cursor = Cursors.SizeAll
    End Sub

    Private Sub Panel1_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Panel1.MouseMove
        If allowCoolMove = True Then
            Panel1.Location = New Point(Panel1.Location.X + e.X - myCoolPoint.X, Panel1.Location.Y + e.Y - myCoolPoint.Y)
        End If
    End Sub

    Private Sub Panel1_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Panel1.MouseUp
        allowCoolMove = False
        Me.Cursor = Cursors.Default
        PanelMoved = True
    End Sub

    Private Sub frmPlayer_Shown(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Shown
        BorderWidth = (Me.Width - Me.ClientSize.Width) / 2
        TitlebarHeight = Me.Height - Me.ClientSize.Height - 2 * BorderWidth
    End Sub

    Private Sub Panel1_Paint(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel1.Paint
        bmpLastScreen = Me.TakeScreenshot(True)
        'bmpLastScreen.Save("c:\vgddplayerlast.png", Imaging.ImageFormat.Png)
    End Sub
End Class