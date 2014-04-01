'This program is free software: you can redistribute it and/or modify
'it under the terms of the GNU General Public License as publishedGetBoolean by
'the Free Software Foundation, either version 3 of the License, or
'(at your option) any later version.

'This program is distributed in the hope that it will be useful,
'but WITHOUT ANY WARRANTY; without even the implied warranty of
'MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'GNU General Public License for more details.
'You should have received a copy of the GNU General Public License
'along with this program.  If not, see <http://www.gnu.org/licenses/>.

Option Strict Off
Imports System.Threading
Imports System.Net
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Input
Imports System.Net.Mail
Imports WindowsHookLib
Imports Microsoft.DirectX.DirectInput

Public Class Form1
    Dim joystickDevice As Device
    Dim updated As Boolean = False, newversion As String = ""
    Dim running As Integer = 1, finishedload As Boolean = False, inisettings As ini, skipsavesettings As Boolean = False
    Dim loglocation As String = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) & "\GTA San Andreas User Files\SAMP"
    Dim wClient As WebClient
    Private trd2 As Thread
    Private UpdateChecker As System.Threading.Thread = New Thread(AddressOf Updater.IsLatest)
    Dim WithEvents kHook As New KeyboardHook, mHook As New MouseHook
    Private Declare Function GetForegroundWindow Lib "user32" Alias "GetForegroundWindow" () As IntPtr
    Private Declare Auto Function GetWindowText Lib "user32" (ByVal hWnd As System.IntPtr, ByVal lpString As System.Text.StringBuilder, ByVal cch As Integer) As Integer
    Dim CurrentVersion As String = "v" & System.Reflection.Assembly.GetEntryAssembly.GetName().Version.ToString
    Dim ProgramName As String = System.Reflection.Assembly.GetEntryAssembly().GetName().Name.Replace(" ", "_")
    Dim keybinderdisabled As Boolean = True, param_obj(2) As Object
    Dim CMDNumber As New Dictionary(Of String, Integer)()
#Region "Re-Usable Subs and Functions"
    'Function used to get title of window
    Private Function GetCaption() As String
        Dim Caption As New System.Text.StringBuilder(256)
        Dim hWnd As IntPtr = GetForegroundWindow()
        GetWindowText(hWnd, Caption, Caption.Capacity)
        Return Caption.ToString()
    End Function

    'Function to check if enter is requested
    Function SendEnter()
        If chkSendEnter.Checked Then Return "{Enter}"
        Return ""
    End Function

    'Function to check if T is requested
    Function SendT()
        If chkSendT.Checked Then Return "t"
        Return ""
    End Function
    'Sub that handles all the splitting and toggling of the commands
    Sub macro(ByVal param_obj() As Object)
        Try
            Dim substr As String = param_obj(1)
            Dim pressed As String = param_obj(0)
            If substr.Contains(txtToggleChar.Text) Then
                Debug.WriteLine("this is trhingy")
                If Not CMDNumber.ContainsKey(pressed) Then CMDNumber(pressed) = 1
                Dim splitstring() As String = Split(substr, txtToggleChar.Text)
                Dim x As Integer = 0
                For Each item In splitstring
                    x = x + 1
                    If x >= CMDNumber(pressed) Then
                        SendKeys.SendWait(SendT() + item + SendEnter())
                        CMDNumber(pressed) = CMDNumber(pressed) + 1
                        If splitstring.GetLength(0) = x Then CMDNumber(pressed) = 1
                        Exit Sub
                    End If
                Next
            Else
                substr = substr.Replace(txtDelayChar.Text, txtMacroChar.Text + txtDelayChar.Text)
                Dim splitstring() As String = Split(substr, txtMacroChar.Text)
                For Each item In splitstring
                    If item.Length > 4 Then
                        If item(0) = txtDelayChar.Text And IsNumeric(item.Substring(1, 4)) = True Then
                            Thread.Sleep(item.Substring(1, 4))
                            item = item.Remove(0, 5)
                        End If
                    End If
                    SendKeys.SendWait(SendT() + item + SendEnter())
                Next
            End If
            keybinderdisabled = False
        Catch ex As Exception
            MsgBox("String contains invalid character!")
        End Try
    End Sub
    'Sub to check key matches pressed key
    Public Sub KeyCheck(checkbox As NSOnOffBox, pressedkey As String, chosenkey As String, cmd As NSTextBox, ByVal e As WindowsHookLib.KeyboardEventArgs)
        trd2 = New Thread(AddressOf macro)
        trd2.IsBackground = True
        If checkbox.Checked = True Then
            If pressedkey = chosenkey Then
                e.Handled = True
                param_obj(1) = cmd.Text
                trd2.Start(param_obj)
            End If
        End If
    End Sub
    'Function to check whether -debug is set
    Function DebugCheck()
        If Environment.GetCommandLineArgs.Length > 1 Then
            For Each x As String In Environment.GetCommandLineArgs
                If x = "-debug" Then Return "GTA:SA:MP"
            Next
        End If
        If inisettings.GetString("Advanced Settings", "Debug", False) Then Return "GTA:SA:MP"
        Return GetCaption()
    End Function
    'Sub to savesettings
    Sub savesettings()
        If finishedload = True Then
            For Each ctrl In Me.Panel1.Controls
                If TypeOf ctrl Is NSTextBox Then inisettings.WriteString("SendKey", ctrl.name.replace("NsTextBox", "Send"), ctrl.Text)
                If TypeOf ctrl Is TextBox Then If Not ctrl.text = Nothing Then inisettings.WriteString("HotKey", ctrl.name.replace("TextBox", "Key"), ctrl.text.ToString)
                If TypeOf ctrl Is NSOnOffBox Then inisettings.WriteString("Activate", ctrl.name.replace("NsOnOffBox", "act"), ctrl.checked.ToString)
            Next
            For Each ctrl In Me.Panel2.Controls
                If TypeOf ctrl Is NSTextBox Then inisettings.WriteString("SendKey", ctrl.name.replace("NsTextBox", "Send"), ctrl.Text)
                If TypeOf ctrl Is TextBox Then If Not ctrl.text = Nothing Then inisettings.WriteString("HotKey", ctrl.name.replace("TextBox", "Key"), ctrl.text.ToString)
                If TypeOf ctrl Is NSOnOffBox Then inisettings.WriteString("Activate", ctrl.name.replace("NsOnOffBox", "act"), ctrl.checked.ToString)
            Next
            For Each ctrl In Me.NsTabControl1.TabPages(2).Controls
                If TypeOf ctrl Is NSTextBox Then inisettings.WriteString("360", ctrl.name.replace("txt", "360"), ctrl.text)
                If TypeOf ctrl Is NSOnOffBox Then inisettings.WriteString("360", ctrl.name.replace("chk", "360act"), ctrl.checked.ToString)
            Next
            For Each ctrl In Me.NsTabControl1.TabPages(3).Controls
                If TypeOf ctrl Is NSTextBox Then inisettings.WriteString("Controller", ctrl.name.replace("txt", ""), ctrl.text)
                If TypeOf ctrl Is NSOnOffBox Then inisettings.WriteString("Controller", ctrl.name.replace("chk", "act"), ctrl.checked.ToString)
            Next
            inisettings.WriteString("Mouse", "LeftClick", txtLMB.Text)
            inisettings.WriteString("Mouse", "RightClick", txtRMB.Text)
            inisettings.WriteString("Mouse", "MiddleClick", txtMMB.Text)
            inisettings.WriteString("Mouse", "WheelUp", txtWheelUp.Text)
            inisettings.WriteString("Mouse", "WheelDown", txtWheelDown.Text)
            inisettings.WriteString("Mouse", "SB1Click", txtSB1.Text)
            inisettings.WriteString("Mouse", "SB2Click", txtSB2.Text)
            inisettings.WriteString("Mouse", "LeftClickActivated", chkLMB.Checked.ToString)
            inisettings.WriteString("Mouse", "RightClickActivated", chkRMB.Checked.ToString)
            inisettings.WriteString("Mouse", "MiddleClickActivated", chkMMB.Checked.ToString)
            inisettings.WriteString("Mouse", "WheelUpActivated", chkWheelUp.Checked.ToString)
            inisettings.WriteString("Mouse", "WheelDownActivated", chkWheelDown.Checked.ToString)
            inisettings.WriteString("Mouse", "SB1ClickActivated", chkSB1.Checked.ToString)
            inisettings.WriteString("Mouse", "SB2ClickActivated", chkSB2.Checked.ToString)
            inisettings.WriteString("Settings", "ShowChangelog", chkShowChangelog.Checked.ToString)
        End If
    End Sub
    'Function to check whethera process is running or not
    Public Function IsProcessRunning(name As String) As Boolean
        For Each clsProcess As Process In Process.GetProcesses()
            If clsProcess.ProcessName.StartsWith(name) Then Return True
        Next
        Return False
    End Function
#End Region
#Region "Binds (Mouse, Scroll, Keyboard and X360)"

    'Sub that is called when button is released
    Private Sub mHook_MouseUp(ByVal sender As Object, ByVal e As WindowsHookLib.MouseEventArgs) Handles mHook.MouseUp
        If chkUseMouseUp.Checked = True Then DoMouseBinds(sender, e)
    End Sub

    'Sub that is called when button is pressed
    Private Sub mHook_MouseDown(ByVal sender As Object, ByVal e As WindowsHookLib.MouseEventArgs) Handles mHook.MouseDown
        If chkUseMouseUp.Checked = False Then DoMouseBinds(sender, e)
    End Sub

    'Sub that is called when a mouse button is pressed then released
    Sub DoMouseBinds(ByVal sender As Object, ByVal e As WindowsHookLib.MouseEventArgs)
        param_obj(0) = e.Button
        trd2 = New Thread(AddressOf macro)
        trd2.IsBackground = True
        If DebugCheck() = "GTA:SA:MP" Then
            If keybinderdisabled = False Then
                If e.Button = Windows.Forms.MouseButtons.Left Then
                    If chkLMB.Checked = True Then
                        param_obj(1) = txtLMB.Text
                        trd2.Start(param_obj)
                    End If
                ElseIf e.Button = Windows.Forms.MouseButtons.Middle Then
                    If chkMMB.Checked = True Then
                        param_obj(1) = txtMMB.Text
                        trd2.Start(param_obj)
                    End If
                ElseIf e.Button = Windows.Forms.MouseButtons.Right Then
                    If chkRMB.Checked = True Then
                        param_obj(1) = txtRMB.Text
                        trd2.Start(param_obj)
                    End If
                ElseIf e.Button = Windows.Forms.MouseButtons.XButton1 Then
                    If chkSB1.Checked = True Then
                        param_obj(1) = txtSB1.Text
                        trd2.Start(param_obj)
                    End If
                ElseIf e.Button = Windows.Forms.MouseButtons.XButton2 Then
                    If chkSB2.Checked = True Then
                        param_obj(1) = txtSB2.Text
                        trd2.Start(param_obj)
                    End If
                End If
            End If
        End If
    End Sub
    'Sub that is called when mouse scroll wheel is turned
    Private Sub mHook_MouseScroll(ByVal sender As Object, ByVal e As WindowsHookLib.MouseEventArgs) Handles mHook.MouseWheel
        param_obj(0) = e.Button
        trd2 = New Thread(AddressOf macro)
        trd2.IsBackground = True
        If DebugCheck() = "GTA:SA:MP" Then
            If keybinderdisabled = False Then
                If e.Delta > 0 Then
                    If chkWheelUp.Checked = True Then
                        param_obj(1) = txtWheelUp.Text
                        trd2.Start(param_obj)
                    End If
                Else
                    If chkWheelDown.Checked = True Then
                        param_obj(1) = txtWheelDown.Text
                        trd2.Start(param_obj)
                    End If
                End If

            End If
        End If
    End Sub

    'Sub that is called when key is released
    Private Sub kHook_KeyUp(ByVal sender As Object, ByVal e As WindowsHookLib.KeyboardEventArgs) Handles kHook.KeyUp
        If chkUseKeyUp.Checked = True Then DoKeybinds(sender, e)
    End Sub

    'Sub that is called when key is pressed
    Private Sub kHook_KeyDown(ByVal sender As Object, ByVal e As WindowsHookLib.KeyboardEventArgs) Handles kHook.KeyDown
        If chkUseKeyUp.Checked = False Then DoKeybinds(sender, e)
    End Sub

    'Sub that is called when a keyboard key is pressed or released
    Private Sub DoKeybinds(ByVal semder As Object, ByVal e As WindowsHookLib.KeyboardEventArgs)
        If DebugCheck() = "GTA:SA:MP" Then
            If keybinderdisabled = False Then
                param_obj(0) = e.KeyData.ToString.ToUpper
                KeyCheck(NsOnOffBox1, param_obj(0), TextBox1.Text.ToUpper, NsTextBox1, e)
                KeyCheck(NsOnOffBox2, param_obj(0), TextBox2.Text.ToUpper, NsTextBox2, e)
                KeyCheck(NsOnOffBox3, param_obj(0), TextBox3.Text.ToUpper, NsTextBox3, e)
                KeyCheck(NsOnOffBox4, param_obj(0), TextBox4.Text.ToUpper, NsTextBox4, e)
                KeyCheck(NsOnOffBox5, param_obj(0), TextBox5.Text.ToUpper, NsTextBox5, e)
                KeyCheck(NsOnOffBox6, param_obj(0), TextBox6.Text.ToUpper, NsTextBox6, e)
                KeyCheck(NsOnOffBox7, param_obj(0), TextBox7.Text.ToUpper, NsTextBox7, e)
                KeyCheck(NsOnOffBox8, param_obj(0), TextBox8.Text.ToUpper, NsTextBox8, e)
                KeyCheck(NsOnOffBox9, param_obj(0), TextBox9.Text.ToUpper, NsTextBox9, e)
                KeyCheck(NsOnOffBox10, param_obj(0), TextBox10.Text.ToUpper, NsTextBox10, e)
                KeyCheck(NsOnOffBox11, param_obj(0), TextBox11.Text.ToUpper, NsTextBox11, e)
                KeyCheck(NsOnOffBox12, param_obj(0), TextBox12.Text.ToUpper, NsTextBox12, e)
                KeyCheck(NsOnOffBox13, param_obj(0), TextBox13.Text.ToUpper, NsTextBox13, e)
                KeyCheck(NsOnOffBox14, param_obj(0), TextBox14.Text.ToUpper, NsTextBox14, e)
                KeyCheck(NsOnOffBox15, param_obj(0), TextBox15.Text.ToUpper, NsTextBox15, e)
                KeyCheck(NsOnOffBox16, param_obj(0), TextBox16.Text.ToUpper, NsTextBox16, e)
                KeyCheck(NsOnOffBox17, param_obj(0), TextBox17.Text.ToUpper, NsTextBox17, e)
                KeyCheck(NsOnOffBox18, param_obj(0), TextBox18.Text.ToUpper, NsTextBox18, e)
                KeyCheck(NsOnOffBox19, param_obj(0), TextBox19.Text.ToUpper, NsTextBox19, e)
                KeyCheck(NsOnOffBox10, param_obj(0), TextBox20.Text.ToUpper, NsTextBox20, e)
            End If
        End If
        If e.KeyData.ToString = "F6" Or e.KeyData.ToString = "T" Or e.KeyData.ToString = "`" Then keybinderdisabled = True
        If e.KeyData.ToString = "Return" Or e.KeyData.ToString = "Escape" Then keybinderdisabled = False
    End Sub
    'Sub timer to control x360 binds (can't use a global hook like keyboard and mouse)
    Dim Prev360Buttons As GamePadState
    Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles timer360.Tick
        If DebugCheck() = "GTA:SA:MP" Then
            trd2 = New Thread(AddressOf macro)
            trd2.IsBackground = True
            Dim currentState As GamePadState = GamePad.GetState(PlayerIndex.One)
            If currentState.IsConnected Then
                If chkButtonA.Checked = True Then
                    If currentState.Buttons.A = ButtonState.Pressed AndAlso Prev360Buttons.Buttons.A = ButtonState.Released Then
                        param_obj(0) = "A"
                        param_obj(1) = txtButtonA.Text
                        trd2.Start(param_obj)
                    End If
                End If
                If chkButtonX.Checked = True Then
                    If currentState.Buttons.X = ButtonState.Pressed AndAlso Prev360Buttons.Buttons.X = ButtonState.Released Then
                        param_obj(0) = "XButton"
                        param_obj(1) = txtButtonX.Text
                        trd2.Start(param_obj)
                    End If
                End If
                If chkButtonY.Checked = True Then
                    If currentState.Buttons.Y = ButtonState.Pressed AndAlso Prev360Buttons.Buttons.Y = ButtonState.Released Then
                        param_obj(0) = "YButton"
                        param_obj(1) = txtButtonY.Text
                        trd2.Start(param_obj)
                    End If
                End If
                If chkButtonB.Checked = True Then
                    If currentState.Buttons.B = ButtonState.Pressed AndAlso Prev360Buttons.Buttons.B = ButtonState.Released Then
                        param_obj(0) = "BButton"
                        param_obj(1) = txtButtonB.Text
                        trd2.Start(param_obj)
                    End If
                End If
                If chkRB.Checked = True Then
                    If currentState.Buttons.RightShoulder = ButtonState.Pressed AndAlso Prev360Buttons.Buttons.RightShoulder = ButtonState.Released Then
                        param_obj(0) = "RB"
                        param_obj(1) = txtRb.Text
                        trd2.Start(param_obj)
                    End If
                End If
                If chkLB.Checked = True Then
                    If currentState.Buttons.LeftShoulder = ButtonState.Pressed AndAlso Prev360Buttons.Buttons.LeftShoulder = ButtonState.Released Then
                        param_obj(0) = "LB"
                        param_obj(1) = txtLb.Text
                        trd2.Start(param_obj)
                    End If
                End If
                If chkDpadDown.Checked = True Then
                    If currentState.DPad.Down = ButtonState.Pressed AndAlso Prev360Buttons.DPad.Down = ButtonState.Released Then
                        param_obj(0) = "DpadDown"
                        param_obj(1) = txtDpadDown.Text
                        trd2.Start(param_obj)
                    End If
                End If
                If chkDpadLeft.Checked = True Then
                    If currentState.DPad.Left = ButtonState.Pressed AndAlso Prev360Buttons.DPad.Left = ButtonState.Released Then
                        param_obj(0) = "DpadLeft"
                        param_obj(1) = txtDpadLeft.Text
                        trd2.Start(param_obj)
                    End If
                End If
                If chkDpadRight.Checked = True Then
                    If currentState.DPad.Right = ButtonState.Pressed AndAlso Prev360Buttons.DPad.Right = ButtonState.Released Then
                        param_obj(0) = "DpadRight"
                        param_obj(1) = txtDpadRight.Text
                        trd2.Start(param_obj)
                    End If
                End If
                If chkDpadUp.Checked = True Then
                    If currentState.DPad.Up = ButtonState.Pressed AndAlso Prev360Buttons.DPad.Up = ButtonState.Released Then
                        param_obj(0) = "DpadUp"
                        param_obj(1) = txtDpadUp.Text
                        trd2.Start(param_obj)
                    End If
                End If
                If chkRightStick.Checked = True Then
                    If currentState.Buttons.RightStick = ButtonState.Pressed AndAlso Prev360Buttons.Buttons.RightStick = ButtonState.Released Then
                        param_obj(0) = "RS"
                        param_obj(1) = txtRightStickPress.Text
                        trd2.Start(param_obj)
                    End If
                End If
                If chkLeftStick.Checked = True Then
                    If currentState.Buttons.LeftStick = ButtonState.Pressed AndAlso Prev360Buttons.Buttons.LeftStick = ButtonState.Released Then
                        param_obj(0) = "LS"
                        param_obj(1) = txtLeftStickPress.Text
                        trd2.Start(param_obj)
                    End If
                End If
                Prev360Buttons = currentState
            End If
        End If
    End Sub
#End Region

    'Button that resets everything and restarts application
    Private Sub btnReset_Click(sender As Object, e As EventArgs) Handles btnReset.Click
        If MsgBox("Are you sure you wish to reset all settings and keybinds?", vbYesNo + MsgBoxStyle.Question, "Confirmation") = vbYes Then
            skipsavesettings = True
            If IO.File.Exists(Application.StartupPath & "\keybinds\" & txtSAMPUsername.Text & "_keybinds.sav") Then IO.File.Delete(Application.StartupPath & "\keybinds\" & txtSAMPUsername.Text & "_keybinds.sav")
            MsgBox("Default settings restored! Application will now restart", vbInformation, "Success!")
            Application.Restart()
        End If
    End Sub

    'Button which then calls the sub savesettings()
    Private Sub btnSave_Click(sender As Object, e As EventArgs)
        savesettings()
    End Sub

    'Button to savesettings() and launch SAMP
    Private Sub btnLaunch_Click(sender As Object, e As EventArgs) Handles btnLaunch.Click
        savesettings()
        Dim gtalocation As String = ""
        If My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\SAMP", "gta_sa_exe", Nothing) Is Nothing Or My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\SAMP", "gta_sa_exe", Nothing) = "" Then
            MsgBox("gta_sa.exe could not be detected automatically, Please manually locate your gta_sa.exe, You only need to do this once.", vbCritical, "File not found")
            Using dialog As New OpenFileDialog
                If dialog.ShowDialog = DialogResult.Cancel Then
                    MsgBox("You did not select a file. Action aborted!", vbCritical, "ERROR")
                    Exit Sub
                Else
                    My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\SAMP", "gta_sa_exe", dialog.FileName)
                    If MsgBox("gta_sa.exe was successfully detected." & vbNewLine & vbNewLine & "Do you want to launch ""San Andreas Cops n Robbers"" now?", vbInformation + MsgBoxStyle.YesNo, "Success") <> MsgBoxResult.Yes Then Exit Sub
                End If
            End Using
        End If
        Process.Start(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\SAMP", "gta_sa_exe", Nothing).Replace("gta_sa.exe", "samp.exe"))
    End Sub

    'Form1 Closing Code
    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If skipsavesettings = False Then savesettings()
        kHook.Dispose()
        mHook.Dispose()
    End Sub

    'Form1 Resize Code
    Private Sub Form1_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        If Me.WindowState = FormWindowState.Minimized Then
            NotifyIcon1.Visible = True
            Me.ShowInTaskbar = False
        End If
    End Sub
    Dim gameControllerList As DeviceList = Manager.GetDevices(DeviceClass.GameControl, EnumDevicesFlags.AttachedOnly)
    'Form1 Shown code
    Private Sub Form1_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        WebBrowser1.Navigate("http://changelog.cyanlabs.co.uk/?product=SAMP_Keybinder_Evolution")
        CheckForIllegalCrossThreadCalls = False
        If Environment.GetCommandLineArgs.Length > 1 Then
            For Each x As String In Environment.GetCommandLineArgs
                If x = "-startup" Then
                    Me.WindowState = FormWindowState.Minimized
                    ShowInTaskbar = False
                    NotifyIcon1.Visible = True
                End If
                If x.Contains("-updated=") Then
                    updated = True
                    newversion = x.Replace("-updated=", "")
                End If
            Next
        End If
        If Not IO.Directory.Exists(loglocation & "\Logs") Then IO.Directory.CreateDirectory(loglocation & "\Logs")
        Try
            kHook.InstallHook()
            mHook.InstallHook()
        Catch ex As Exception
            MessageBox.Show("Failed to install the hooks!")
        End Try
        txtSAMPUsername.Text = My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\SAMP", "PlayerName", "Keybinds")
        If Not IO.Directory.Exists(Application.StartupPath & "\keybinds") Then IO.Directory.CreateDirectory(Application.StartupPath & "\keybinds")
        If IO.File.Exists(Application.StartupPath & "\keybinds.sav") Then
            IO.File.Copy(Application.StartupPath & "\keybinds.sav", Application.StartupPath & "\keybinds\" & txtSAMPUsername.Text & "_keybinds.sav", True)
            IO.File.Delete(Application.StartupPath & "\keybinds.sav")
        End If
        Dim di As New IO.DirectoryInfo(Application.StartupPath & "\keybinds")
        Dim fi As IO.FileInfo() = di.GetFiles("*.sav")
        For Each file In fi
            If Not file.Name.ToString = "_keybinds.sav" Then cmbSAMPUsername.Items.Add(file.Name.ToString.Replace("_keybinds.sav", ""))
        Next

        inisettings = New ini(Application.StartupPath & "\keybinds\" & txtSAMPUsername.Text & "_keybinds.sav")
        If inisettings.GetString("Settings", "AutoUpdate", False) = True Then
            If updated Then
                MsgBox("You have successfully updated to V" & newversion, MsgBoxStyle.Information, "Update Successful")
            Else
                UpdateChecker.IsBackground = True
                UpdateChecker.Start()
            End If
        End If
        For Each ctrl In Me.Panel1.Controls
            If TypeOf ctrl Is NSTextBox Then ctrl.text = inisettings.GetString("SendKey", ctrl.name.replace("NsTextBox", "Send"), ctrl.text)
            If TypeOf ctrl Is TextBox Then ctrl.text = inisettings.GetString("HotKey", ctrl.name.replace("TextBox", "Key"), "")
            If TypeOf ctrl Is NSOnOffBox Then ctrl.checked = inisettings.GetString("Activate", ctrl.name.replace("NsOnOffBox", "act"), False)
        Next
        For Each ctrl In Me.Panel2.Controls
            If TypeOf ctrl Is NSTextBox Then ctrl.text = inisettings.GetString("SendKey", ctrl.name.replace("NsTextBox", "Send"), ctrl.text)
            If TypeOf ctrl Is TextBox Then ctrl.text = inisettings.GetString("HotKey", ctrl.name.replace("TextBox", "Key"), "")
            If TypeOf ctrl Is NSOnOffBox Then ctrl.checked = inisettings.GetString("Activate", ctrl.name.replace("NsOnOffBox", "act"), False)
        Next
        For Each ctrl In Me.NsTabControl1.TabPages(2).Controls
            If TypeOf ctrl Is NSTextBox Then ctrl.text = inisettings.GetString("360", ctrl.name.replace("txt", "360"), ctrl.text)
            If TypeOf ctrl Is NSOnOffBox Then ctrl.checked = inisettings.GetString("360", ctrl.name.replace("chk", "360act"), False)
        Next
        For Each ctrl In Me.NsTabControl1.TabPages(3).Controls
            If TypeOf ctrl Is NSTextBox Then ctrl.text = inisettings.GetString("Controller", ctrl.name.replace("txt", ""), ctrl.text)
            If TypeOf ctrl Is NSOnOffBox Then ctrl.checked = inisettings.GetString("Controller", ctrl.name.replace("chk", "act"), ctrl.checked.ToString)
        Next
        txtLMB.Text = inisettings.GetString("Mouse", "LeftClick", Nothing)
        txtRMB.Text = inisettings.GetString("Mouse", "RightClick", Nothing)
        txtMMB.Text = inisettings.GetString("Mouse", "MiddleClick", Nothing)
        txtWheelUp.Text = inisettings.GetString("Mouse", "WheelUp", Nothing)
        txtWheelDown.Text = inisettings.GetString("Mouse", "WheelDown", Nothing)
        txtSB1.Text = inisettings.GetString("Mouse", "SB1Click", Nothing)
        txtSB2.Text = inisettings.GetString("Mouse", "SB2Click", Nothing)
        chkLMB.Checked = inisettings.GetString("Mouse", "LeftClickActivated", False)
        chkRMB.Checked = inisettings.GetString("Mouse", "RightClickActivated", False)
        chkMMB.Checked = inisettings.GetString("Mouse", "MiddleClickActivated", False)
        chkWheelUp.Checked = inisettings.GetString("Mouse", "WheelUpActivated", False)
        chkWheelDown.Checked = inisettings.GetString("Mouse", "WheelDownActivated", False)
        chkSB1.Checked = inisettings.GetString("Mouse", "SB1ClickActivated", False)
        chkSB2.Checked = inisettings.GetString("Mouse", "SB2ClickActivated", False)
        chkAutoUpdates.Checked = inisettings.GetString("Settings", "AutoUpdate", False)
        chkEnableLogs.Checked = inisettings.GetString("Settings", "EnableLogManager", False)
        chkEnable360.Checked = inisettings.GetString("360", "MasterToggle", False)
        chkEnablePc.Checked = inisettings.GetString("Controller", "MasterToggle", False)
        chkShowChangelog.Checked = inisettings.GetString("Settings", "ShowChangelog", True)
        chkUseMouseUp.Checked = inisettings.GetString("Advanced Settings", "UseKeyUp", False)
        chkUseKeyUp.Checked = inisettings.GetString("Advanced Settings", "UseMouseUp", False)
        chkDebug.Checked = inisettings.GetString("Advanced Settings", "Debug", False)
        txtMacroChar.Text = inisettings.GetString("Advanced Settings", "MacroChar", "*")
        txtToggleChar.Text = inisettings.GetString("Advanced Settings", "ToggleChar", "#")
        txtDelayChar.Text = inisettings.GetString("Advanced Settings", "DelayChar", "¬")
        chkSendT.Checked = inisettings.GetString("Advanced Settings", "SendT", True)
        chkSendEnter.Checked = inisettings.GetString("Advanced Settings", "SendEnter", True)
        lblVersion.Text = CurrentVersion.ToString
        If chkEnable360.Checked = True Then timer360.Start()
        If chkEnableLogs.Checked = True Then timerLogs.Start()
        If (gameControllerList.Count > 0) Then
            gameControllerList.MoveNext()
            Dim deviceInstance As DeviceInstance = gameControllerList.Current
            joystickDevice = New Device(deviceInstance.InstanceGuid)
            joystickDevice.SetCooperativeLevel(Me, CooperativeLevelFlags.Background Or CooperativeLevelFlags.NonExclusive)
            joystickDevice.SetDataFormat(DeviceDataFormat.Joystick)
            joystickDevice.Acquire()
            joystickDevice.Poll()
        End If
        If chkEnablePc.Checked = True > 0 Then timerPC.Start()
        If My.Computer.Registry.CurrentUser.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Run", True).GetValue(Application.ProductName) Is Nothing Then chkStartup.Checked = False
        finishedload = True
    End Sub

    'Code to filter key presses and make sure it gets the raw value
    Private Sub TextBox2_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox9.KeyDown, TextBox8.KeyDown, TextBox7.KeyDown, TextBox6.KeyDown, TextBox5.KeyDown, TextBox4.KeyDown, TextBox3.KeyDown, TextBox20.KeyDown, TextBox2.KeyDown, TextBox19.KeyDown, TextBox18.KeyDown, TextBox17.KeyDown, TextBox16.KeyDown, TextBox15.KeyDown, TextBox14.KeyDown, TextBox13.KeyDown, TextBox12.KeyDown, TextBox11.KeyDown, TextBox10.KeyDown, TextBox1.KeyDown
        sender.tag = e.KeyCode
        sender.text = e.KeyCode.ToString.ToUpper
        e.SuppressKeyPress = True
    End Sub

    'Autoupdate check change
    Private Sub chkAutoupdates_CheckedChanged(sender As Object)
        Try
            inisettings.WriteString("Settings", "AutoUpdate", sender.checked.ToString)
        Catch ex As NullReferenceException
        End Try
    End Sub

    'Code to monitor whether samp is still running or not, if it isn't save log
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles timerLogs.Tick
        If IsProcessRunning("gta_sa") = False AndAlso running = 2 Then Me.running = 0
        If (IsProcessRunning("gta_sa") = True) Then Me.running = 2
        If Me.running = 0 Then
            Dim str As String = (DateTime.Now.ToString("dd-MM-yy") & "_" & DateTime.Now.ToString("HH-mm"))
            If My.Computer.FileSystem.FileExists(loglocation & "\chatlog.txt") Then
                If Not IO.Directory.Exists(loglocation & "\Logs\" & cmbSAMPUsername.Text) Then IO.Directory.CreateDirectory(loglocation & "\Logs\" & cmbSAMPUsername.Text)
                My.Computer.FileSystem.CopyFile(loglocation & "\chatlog.txt", loglocation & "\Logs\" & cmbSAMPUsername.Text & "\chatlog_" & str & ".txt", True)
                Me.NotifyIcon1.ShowBalloonTip(5000, "SAMP Keybinder Evolution", "Log Saved (" & str & ")", ToolTipIcon.Info)
                Me.running = 1
            End If
        End If
    End Sub

    'Logmanager status
    Private Sub chkEnableLogs_CheckedChanged(sender As Object) Handles chkEnableLogs.CheckedChanged
        Try
            inisettings.WriteString("Settings", "EnableLogManager", chkEnableLogs.Checked)
        Catch ex As NullReferenceException
        End Try
        If sender.Checked = True Then
            timerLogs.Start()
        Else
            timerLogs.Stop()
        End If
    End Sub

    'Change active profile and restart application
    Private Sub btnSaveRestart_Click(sender As Object, e As EventArgs) Handles btnSaveRestart.Click
        Dim result = MsgBox("This will change the SAMP username." & vbNewLine & "All settings and keybinds will be saved as 'OLDNAME_Keybinds.sav' and a new file called '" & cmbSAMPUsername.Text & "_keybinds.sav' will be used. You can switch back to your old username at any time by changing this textbox back." & vbNewLine & vbNewLine & "Are you sure you want to change SAMP Username?", vbYesNo + MsgBoxStyle.Question, "Confirmation")
        If result = vbYes Then
            My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\SAMP", "PlayerName", cmbSAMPUsername.SelectedItem.ToString)
            Application.Restart()
        End If
    End Sub
    '    'Enable/Disable 360 bind timer
    Private Sub chkEnable360_CheckedChanged(sender As Object) Handles chkEnable360.CheckedChanged
        Try
            inisettings.WriteString("360", "MasterToggle", chkEnable360.Checked.ToString)
            If sender.Checked = True Then
                timer360.Start()
            Else
                timer360.Stop()
            End If
            If chkEnablePc.Checked Then
                chkEnablePc.Checked = False
            End If
        Catch ex As NullReferenceException
        End Try
    End Sub

    'Simply opens log folder in explorer
    Private Sub btnLogs_Click(sender As Object, e As EventArgs) Handles btnLogs.Click
        Try
            Process.Start("explorer.exe", loglocation & "\Logs\" & cmbSAMPUsername.Text)
        Catch ex As Exception
            MsgBox("Log directory could not be opened as the directory does not seem to exist.", MsgBoxStyle.Critical, "Error")
        End Try
    End Sub

    'Sends email with feedback/suggestion/bug report
    Private Sub btnSendRequest_Click(sender As Object, e As EventArgs) Handles btnSendRequest.Click
        If txtFeedback.Text = "Leave feedback or suggest a new feature or change here." Or txtFeedback.Text = "" Then Exit Sub
        Dim emailcontents As String = txtFeedback.Text
        Dim result = MsgBox("This will send the feedback below to CyanLabs (Fma965)." & vbNewLine & vbNewLine & """" & emailcontents & """" & vbNewLine & vbNewLine & "Are you sure?", vbYesNo + MsgBoxStyle.Question, "Confirmation")
        If result = vbYes Then
            result = MsgBox("Do you want to include your SA-MP username with the email?", vbYesNo + MsgBoxStyle.Question, "Confirmation")
            If result = vbYes Then emailcontents &= vbNewLine & vbNewLine & "Feedback/Suggestion was posted by """ & cmbSAMPUsername.Text & """"
            Try
                Dim SmtpServer As New SmtpClient()
                Dim mail As New MailMessage()
                SmtpServer.Port = 2525
                SmtpServer.Host = "smtpcorp.com"
                mail = New MailMessage()
                mail.From = New MailAddress("sacnrkeybinder2013@cyanlabs.co.uk")
                mail.To.Add("fma96580@gmail.com")
                mail.Subject = "SAMP Keybinder Evolution - Feedback and Suggestions"
                mail.Body = "New feedback or suggestion for 'SAMP Keybinder Evolution' has been recieved!" & vbNewLine & vbNewLine & emailcontents
                SmtpServer.Send(mail)
                MsgBox("Your feedback has been sent successfully, Thank you for helping make SAMP Keybinder Evolution better!")
            Catch ex As Exception
                MsgBox("The following error occured:" & vbNewLine & vbNewLine & ex.ToString, MsgBoxStyle.Critical, "Error")
            End Try
        End If
    End Sub

    'Clears textbox contents when clicked if value is default
    Private Sub txtFeedback_Enter(sender As Object, e As EventArgs) Handles txtFeedback.Enter
        If sender.text = "Leave feedback or suggest a new feature or change here." Then sender.text = ""
    End Sub

    'Add/Remove registry entry to start at windows startup
    Private Sub chkStartup_CheckedChanged(sender As Object) Handles chkStartup.CheckedChanged
        If sender.checked = True Then
            My.Computer.Registry.CurrentUser.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Run", True).SetValue(Application.ProductName, Application.ExecutablePath & " -startup")
        Else
            If Not My.Computer.Registry.CurrentUser.GetValue("SOFTWARE\Microsoft\Windows\CurrentVersion\Run") Is Nothing Then My.Computer.Registry.CurrentUser.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Run", True).DeleteValue(Application.ProductName)
        End If
    End Sub

    'Code that runs when notification icon is double clicked
    Private Sub NotifyIcon1_MouseDoubleClick(sender As Object, e As Windows.Forms.MouseEventArgs) Handles NotifyIcon1.MouseDoubleClick
        NotifyIcon1.Visible = False
        ShowInTaskbar = True
        Me.Show()
        Me.WindowState = FormWindowState.Normal
    End Sub

    'Saves all advanced checkbox settings
    Private Sub chkAdvancedSettings_CheckedChanged(sender As Object) Handles chkUseMouseUp.CheckedChanged, chkUseKeyUp.CheckedChanged, chkSendT.CheckedChanged, chkSendEnter.CheckedChanged, chkDebug.CheckedChanged
        Try
            inisettings.WriteString("Advanced Settings", sender.name.replace("chk", ""), sender.checked.ToString)
        Catch ex As NullReferenceException
        End Try
    End Sub

    '    'Saves all advanced textbox settings
    Private Sub txtChars_Leave(sender As Object, e As EventArgs) Handles txtToggleChar.Leave, txtMacroChar.Leave, txtDelayChar.Leave
        Try
            inisettings.WriteString("Advanced Settings", sender.name.replace("txt", ""), sender.text)
        Catch ex As NullReferenceException
        End Try
    End Sub
    Private Sub NsPaginator1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles NsPaginator1.SelectedIndexChanged
        If NsPaginator1.SelectedIndex = 1 Then
            Panel1.Visible = False
            Panel2.Visible = True
        Else
            Panel1.Visible = True
            Panel2.Visible = False
        End If
    End Sub

    Private Sub NsButton5_Click(sender As Object, e As EventArgs) Handles NsButton5.Click
        If Panel3.Visible Then
            sender.text = "Show advanced settings"
            Panel3.Visible = False
        Else
            sender.text = "Hide advanced settings"
            Panel3.Visible = True
        End If
    End Sub

    Private Sub NsButton7_Click(sender As Object, e As EventArgs) Handles NsButton7.Click
        If Panel4.Visible Then
            sender.text = "Show changelog"
            Panel4.Visible = False
        Else
            sender.text = "Hide changelog"
            Panel4.Visible = True
        End If
    End Sub
    Private Sub btnAddUser_Click(sender As Object, e As EventArgs) Handles btnAddUser.Click
        cmbSAMPUsername.Items.Add(txtSAMPUsername.Text)
    End Sub

    Private Sub CBtnClose_Click(sender As Object, e As EventArgs) Handles CBtnClose.Click
        savesettings()
    End Sub

    Private Sub NsButton1_Click(sender As Object, e As EventArgs)
        MsgBox(My.Application.Info.AssemblyName.Replace(" ", "_"))
    End Sub

    Private Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem.Click
        Me.Close()
    End Sub

    Private Sub ShowToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ShowToolStripMenuItem.Click
        NotifyIcon1.Visible = False
        ShowInTaskbar = True
        Me.Show()
        Me.WindowState = FormWindowState.Normal
    End Sub
    Dim prevPCButtons
    Private Sub timerPC_Tick(sender As Object, e As EventArgs) Handles timerPC.Tick
        If (gameControllerList.Count > 0) Then
            Try
                If DebugCheck() = "GTA:SA:MP" Then
                    trd2 = New Thread(AddressOf macro)
                    trd2.IsBackground = True
                    Dim state As JoystickState = joystickDevice.CurrentJoystickState
                    If chkPCButton1.Checked = True Then
                        If state.GetButtons(0) = 128 AndAlso prevPCButtons(0) = 0 Then
                            param_obj(0) = "PC1"
                            param_obj(1) = txtPCButton1.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    If chkPCButton2.Checked = True Then
                        If state.GetButtons(1) = 128 AndAlso prevPCButtons(1) = 0 Then
                            param_obj(0) = "PC2"
                            param_obj(1) = txtPCButton2.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    If chkPCButton3.Checked = True Then
                        If state.GetButtons(2) = 128 AndAlso prevPCButtons(2) = 0 Then
                            param_obj(0) = "PC3"
                            param_obj(1) = txtPCButton3.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    If chkPCButton4.Checked = True Then
                        If state.GetButtons(3) = 128 AndAlso prevPCButtons(3) = 0 Then
                            param_obj(0) = "PC4"
                            param_obj(1) = txtPCButton4.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    If chkPCButton5.Checked = True Then
                        If state.GetButtons(4) = 128 AndAlso prevPCButtons(4) = 0 Then
                            param_obj(0) = "PC5"
                            param_obj(1) = txtPCButton5.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    If chkPCButton6.Checked = True Then
                        If state.GetButtons(5) = 128 AndAlso prevPCButtons(5) = 0 Then
                            param_obj(0) = "PC6"
                            param_obj(1) = txtPCButton6.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    If chkPCButton7.Checked = True Then
                        If state.GetButtons(6) = 128 AndAlso prevPCButtons(6) = 0 Then
                            param_obj(0) = "PC7"
                            param_obj(1) = txtPCButton7.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    If chkPCButton8.Checked = True Then
                        If state.GetButtons(7) = 128 AndAlso prevPCButtons(7) = 0 Then
                            param_obj(0) = "PC8"
                            param_obj(1) = txtPCButton8.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    If chkPCButton9.Checked = True Then
                        If state.GetButtons(8) = 128 AndAlso prevPCButtons(8) = 0 Then
                            param_obj(0) = "PC9"
                            param_obj(1) = txtPCButton9.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    If chkPCButton10.Checked = True Then
                        If state.GetButtons(9) = 128 AndAlso prevPCButtons(9) = 0 Then
                            param_obj(0) = "PC10"
                            param_obj(1) = txtPCButton10.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    If chkPCButton11.Checked = True Then
                        If state.GetButtons(10) = 128 AndAlso prevPCButtons(10) = 0 Then
                            param_obj(0) = "PC11"
                            param_obj(1) = txtPCButton11.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    If chkPCButton12.Checked = True Then
                        If state.GetButtons(11) = 128 AndAlso prevPCButtons(11) = 0 Then
                            param_obj(0) = "PC12"
                            param_obj(1) = txtPCButton12.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    prevPCButtons = state.GetButtons
                End If
            Catch ex As Exception
            End Try
        End If
    End Sub

    Private Sub chkEnablePc_CheckedChanged(sender As Object) Handles chkEnablePc.CheckedChanged
        Try
            inisettings.WriteString("Controller", "MasterToggle", chkEnablePc.Checked.ToString)
            If chkEnablePc.Checked = True Then
                timerPC.Start()
            Else
                timerPC.Stop()
            End If
            If chkEnable360.Checked Then
                chkEnable360.Checked = False
            End If
        Catch ex As NullReferenceException
        End Try
    End Sub
End Class
