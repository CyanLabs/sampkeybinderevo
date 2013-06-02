﻿Public Module Updater
    Dim product As String = My.Application.Info.AssemblyName.Replace(" ", "_")
    Dim wc As New Net.WebClient
    Dim localversion As String = My.Application.Info.Version.ToString
    Public Sub IsLatest()
        If Environment.GetCommandLineArgs.Length > 1 Then
            For Each x As String In Environment.GetCommandLineArgs
                If x.Contains("-v=") Then localversion = x.Replace("-v=", "")
            Next
        End If
        Try
            Dim latestversion As String = wc.DownloadString("http://downloads.cyanlabs.co.uk/version.php?product=" & product).Replace(" ", "")
            If localversion < latestversion Then
                Dim changelog As String = wc.DownloadString("http://changelog.cyanlabs.co.uk?product=" & product & "&from=" & localversion & "&to=" & latestversion)
                If changelog.Length > 11 Then
                    changelog = changelog.Substring(0, changelog.Length - 11).Replace("<br/><br/>", vbNewLine & vbNewLine).Replace("<br/>", "")
                Else
                    changelog = changelog.Replace("<br/><br/>", vbNewLine & vbNewLine).Replace("<br/>", "")
                End If
                If MsgBox("A new update is available for download" & vbNewLine & vbNewLine & "Would you like to download v" & latestversion & "?" & vbNewLine & vbNewLine & changelog, MsgBoxStyle.Information + MsgBoxStyle.YesNo, "Update Avaliable") = vbYes Then
                    If IO.File.Exists(Application.StartupPath & "\AutoUpdater.exe") Then
                        Dim updaterversion As FileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(Application.StartupPath & "\AutoUpdater.exe")
                        If updaterversion.FileVersion < wc.DownloadString("http://downloads.cyanlabs.co.uk/version.php?product=AutoUpdater").Replace(" ", "") Then
                            DownloadUpdater(latestversion)
                        Else
                            Process.Start(Application.StartupPath & "\AutoUpdater.exe ", "-product=" & product & " -v=" & latestversion)
                            Application.Exit()
                        End If
                    Else
                        DownloadUpdater(latestversion)
                    End If
                End If
            End If
        Catch ex As Net.WebException
            MsgBox(ex.Message.ToString)
        Catch ex As NullReferenceException
            MsgBox("This application currently doesn't support autoupdating")
        Catch ex As System.Xml.XPath.XPathException
            MsgBox(ex.Message.ToString)
        End Try
    End Sub
    Private Sub DownloadUpdater(ByVal latestversion As String)
        Try
            If IO.File.Exists(Application.StartupPath & "\AutoUpdater.exe") Then IO.File.Delete(Application.StartupPath & "\AutoUpdater.exe")
            wc.DownloadFile(New Uri("http://downloads.cyanlabs.co.uk/AutoUpdater/AutoUpdater.exe"), Application.StartupPath & "\AutoUpdater.exe")
            Process.Start(Application.StartupPath & "\AutoUpdater.exe ", "-product=" & product & " -v=" & latestversion)
            Debug.Print(4)
            Application.Exit()
        Catch ex As Net.WebException
            MsgBox(ex.Message.ToString)
        End Try
    End Sub
End Module
