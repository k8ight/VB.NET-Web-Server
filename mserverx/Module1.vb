Imports System
Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Web
Imports System.Net.Sockets
Imports System.Threading
Module module1


    Sub Main()
        Dim objStreamReader As StreamReader
        Dim pname As String
        Dim ipb As String
        objStreamReader = New StreamReader("conf\config.ini")
        pname = objStreamReader.ReadLine
        Dim portx As String() = pname.Split("=")
        Console.WriteLine("Selected Port:" + portx(1))

        ipb = objStreamReader.ReadLine
        Dim ipbx As String() = ipb.Split("=")
        Console.WriteLine("Selected IP:" + ipbx(1))
    

        Try
            Dim hostName As String = Dns.GetHostName()
            Dim serverIP As IPAddress = IPAddress.Parse(ipbx(1))

            ' Web Server Port = 80
            Dim Port As String = portx(1)

            Dim tcpListener As New TcpListener(serverIP, Int32.Parse(Port))

            tcpListener.Start()

            Console.WriteLine("Web server started at: " & serverIP.ToString() & ":" & Port)

            Dim httpSession As New HTTPSession(tcpListener)

            Dim serverThread As New Thread(New ThreadStart(AddressOf httpSession.ProcessThread))

            serverThread.Start()

        Catch ex As Exception
            Console.WriteLine(ex.StackTrace.ToString())
        End Try
    End Sub





    Public Class HTTPSession
        Private tcpListener As System.Net.Sockets.TcpListener
        Private clientSocket As System.Net.Sockets.Socket

        Public Sub New(ByVal tcpListener As System.Net.Sockets.TcpListener)
            Me.tcpListener = tcpListener
        End Sub

        Public Sub ProcessThread()
            While (True)
                Try
                    clientSocket = tcpListener.AcceptSocket()

                    ' Socket Information
                    Dim clientInfo As IPEndPoint = CType(clientSocket.RemoteEndPoint, IPEndPoint)

                    Console.WriteLine("Client: " + clientInfo.Address.ToString() + ":" + clientInfo.Port.ToString())

                    ' Set Thread for each Web Browser Connection
                    Dim clientThread As New Thread(New ThreadStart(AddressOf ProcessRequest))

                    clientThread.Start()

                Catch ex As Exception
                    Console.WriteLine(ex.StackTrace.ToString())

                    If clientSocket.Connected Then
                        clientSocket.Close()
                    End If

                End Try
            End While
        End Sub

        Protected Sub ProcessRequest()
            Dim recvBytes(1024) As Byte
            Dim htmlReq As String = Nothing
            Dim bytes As Int32
            Dim oReader As StreamReader
            Dim aname As String
            Dim bname As String
            Dim fname As String
            Dim iname As String
            oReader = New StreamReader("conf\config.ini")
            aname = oReader.ReadLine
            bname = oReader.ReadLine

            fname = oReader.ReadLine
            Dim dname As String() = fname.Split("=")
            iname = oReader.ReadLine
            Dim idname As String() = iname.Split("=")


            Try
                ' Receive HTTP Request from Web Browser
                bytes = clientSocket.Receive(recvBytes, 0, clientSocket.Available, SocketFlags.None)
                htmlReq = Encoding.ASCII.GetString(recvBytes, 0, bytes)

                Console.WriteLine("HTTP Request: ")
                Console.WriteLine(htmlReq)

                ' Set WWW Root Path
                Dim rootPath As String = Directory.GetCurrentDirectory() & dname(1)

                ' Set default page
                Dim defaultPage As String = idname(1)

                Dim strArray() As String
                Dim strRequest As String

                strArray = htmlReq.Trim.Split(" ")

                ' Determine the HTTP method (GET only)
                If strArray(0).Trim().ToUpper.Equals("GET") Then
                    strRequest = strArray(1).Trim

                    If (strRequest.StartsWith("/")) Then
                        strRequest = strRequest.Substring(1)
                    End If

                    If (strRequest.EndsWith("/") Or strRequest.Equals("")) Then
                        strRequest = strRequest & defaultPage
                    End If

                    strRequest = rootPath & strRequest

                    sendHTMLResponse(strRequest)

                Else ' Not HTTP GET method
                    strRequest = rootPath & "Error\" & "400.html"

                    sendHTMLResponse(strRequest)
                End If

            Catch ex As Exception
                Console.WriteLine(ex.StackTrace.ToString())

                If clientSocket.Connected Then
                    clientSocket.Close()
                End If
            End Try
        End Sub

        ' Send HTTP Response
        '  Private Sub sendHTMLResponse(ByVal httpRequest As String)
        '   Try
        ' Get the file content of HTTP Request 
        '    Dim streamReader As StreamReader = New StreamReader(HttpRequest)
        '   Dim strBuff As String = StreamReader.ReadToEnd()
        '          streamReader.Close()
        '          streamReader = Nothing

        ' The content Length of HTTP Request

        'Dim respByte() As Byte = Encoding.ASCII.GetBytes(strBuff)

        ' Set HTML Header
        '   Dim htmlHeader As String = _
        '     "HTTP/1.0 200 OK" & ControlChars.CrLf & _
        '      "Server: WebServer 1.0" & ControlChars.CrLf & _
        '      "Content-Length: " & respByte.Length & ControlChars.CrLf & _
        '      "Content-Type: " & getContentType(HttpRequest) & _
        '      ControlChars.CrLf & ControlChars.CrLf

        ' The content Length of HTML Header
        '   Dim headerByte() As Byte = Encoding.ASCII.GetBytes(htmlHeader)
        '
        '    Console.WriteLine("HTML Header: " & ControlChars.CrLf & htmlHeader)

        ' Send HTML Header back to Web Browser
        '     clientSocket.Send(headerByte, 0, headerByte.Length, SocketFlags.None)

        ' Send HTML Content back to Web Browser
        '  clientSocket.Send(respByte, 0, respByte.Length, SocketFlags.None)
        '
        ' Close HTTP Socket connection
        '   clientSocket.Shutdown(SocketShutdown.Both)
        '  clientSocket.Close()
        '
        '  Catch ex As Exception
        '    Console.WriteLine(ex.StackTrace.ToString())

        '  If clientSocket.Connected Then
        ' clientSocket.Close()
        ' End If
        ' End Try
        '  End Sub

        ' Get Content Type'
        Private Sub sendHTMLResponse(ByVal httpRequest As String)
            Try
                Dim httpRequest2 As String = httpRequest
                ' Get the file content of HTTP Request 
                If httpRequest.EndsWith(".php") Then
                    Dim txttmpLoc As String = Directory.GetCurrentDirectory() & "\tmp\phpcompiled.cache"
                    ' httpRequest2 = txttmpLoc
                    Try
                        Dim prcProcess As New Process()
                        prcProcess.StartInfo.FileName = "cmd.exe"
                        prcProcess.StartInfo.UseShellExecute = False
                        prcProcess.StartInfo.CreateNoWindow = True
                        prcProcess.StartInfo.RedirectStandardOutput = True
                        prcProcess.StartInfo.RedirectStandardInput = True
                        prcProcess.StartInfo.RedirectStandardError = True
                        prcProcess.Start()
                        Dim swrInput As IO.StreamWriter = prcProcess.StandardInput
                        swrInput.AutoFlush = True
                        swrInput.Write("PHP\PHP-cgi -n -f " & httpRequest2 & ">" & txttmpLoc & System.Environment.NewLine)
                        swrInput.Write("exit" & System.Environment.NewLine)
                        prcProcess.WaitForExit()
                        If Not prcProcess.HasExited Then
                            prcProcess.Kill()
                        End If
                        swrInput.Flush()
                        httpRequest2 = txttmpLoc
                    Catch ex As Exception
                        MsgBox(ex.Message)
                    End Try
                End If
                Dim streamReader As StreamReader = New StreamReader(httpRequest2)
                Dim strBuff As String = streamReader.ReadToEnd()
                streamReader.Close()
                streamReader = Nothing



                ' The content Length of HTTP Request
                Dim respByte() As Byte = Encoding.ASCII.GetBytes(strBuff)

                ' Set HTML Header
                Dim htmlHeader As String = _
                 "HTTP/1.0 200 OK" & ControlChars.CrLf & _
                 "Server: WebServer 1.0" & ControlChars.CrLf & _
                 "Content-Length: " & respByte.Length & ControlChars.CrLf & _
                 "Content-Type: " & getContentType(httpRequest) & _
                 ControlChars.CrLf & ControlChars.CrLf

                ' The content Length of HTML Header
                Dim headerByte() As Byte = Encoding.ASCII.GetBytes(htmlHeader)

                Console.WriteLine("HTML Header: " & ControlChars.CrLf & htmlHeader)

                ' Send HTML Header back to Web Browser
                clientSocket.Send(headerByte, 0, headerByte.Length, SocketFlags.None)

                ' Send HTML Content back to Web Browser
                clientSocket.Send(respByte, 0, respByte.Length, SocketFlags.None)

                ' Close HTTP Socket connection
                clientSocket.Shutdown(SocketShutdown.Both)
                clientSocket.Close()

            Catch ex As Exception
                Console.WriteLine(ex.StackTrace.ToString())

                If clientSocket.Connected Then
                    clientSocket.Close()
                End If
            End Try
        End Sub
        Private Function getContentType(ByVal httpRequest As String) As String
            If (httpRequest.EndsWith("html")) Then
                Return "text/html"
            ElseIf (httpRequest.EndsWith("htm")) Then
                Return "text/html"
            ElseIf (httpRequest.EndsWith("txt")) Then
                Return "text/plain"
            ElseIf (httpRequest.EndsWith("gif")) Then
                Return "image/gif"
            ElseIf (httpRequest.EndsWith("jpg")) Then
                Return "image/jpeg"
            ElseIf (httpRequest.EndsWith("jpeg")) Then
                Return "image/jpeg"
            ElseIf (httpRequest.EndsWith("pdf")) Then
                Return "application/pdf"
            ElseIf (httpRequest.EndsWith("pdf")) Then
                Return "application/pdf"
            ElseIf (httpRequest.EndsWith("doc")) Then
                Return "application/msword"
            ElseIf (httpRequest.EndsWith("xls")) Then
                Return "application/vnd.ms-excel"
            ElseIf (httpRequest.EndsWith("ppt")) Then
                Return "application/vnd.ms-powerpoint"
            ElseIf (httpRequest.EndsWith("php")) Then
                Return "text/html"
            Else
                Return "text/plain"
            End If
        End Function
    End Class
End Module